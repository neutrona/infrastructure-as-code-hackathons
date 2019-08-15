package main

import (
	"bufio"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"log"
	"math"
	"net"
	"net/http"
	"os"
	"sync"
	"time"

	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"github.com/streadway/amqp"
)

type lspData struct {
	Bandwidth float32 `json:"bandwidth"`
	LSP       lsp     `json:"lsp"`
	RRO       []rro   `json:"rro"`
}

type flags struct {
	Administrative bool `json:"administrative"`
	Delegate       bool `json:"delegate"`
	Operational    bool `json:"operational"`
	Remove         bool `json:"remove"`
	Sync           bool `json:"sync"`
}
type lsp struct {
	Extended_tunnel_id           uint32 `json:"extended_tunnel_id"`
	Flags                        flags  `json:"flags"`
	Ipv4_tunnel_endpoint_address net.IP `json:"ipv4_tunnel_endpoint_address"`
	Ipv4_tunnel_sender_address   net.IP `json:"ipv4_tunnel_sender_address"`
	Lsp_id                       uint16 `json:"lsp_id"`
	PCC                          string `json:"pcc"`
	Plsp_id                      uint32 `json:"plsp_id"`
	Symbolic_path_name           string `json:"symbolic_path_name"`
	Tunnel_id                    uint16 `json:"tunnel_id"`
}
type rro struct {
	Address                    net.IP `json:"address"`
	Local_protection_available bool   `json:"local_protection_available"`
	Local_protection_in_use    bool   `json:"local_protection_in_use"`
	Prefix_length              byte   `json:"prefix_length"`
}

var pcepBindingAddress = os.Getenv("PCEP_BINDING_ADDRESS")
var pcepBindingPort = os.Getenv("PCEP_BINDING_PORT")
var rmqHost = os.Getenv("RMQ_HOST")
var rmqPort = os.Getenv("RMQ_PORT")
var rmqUser = os.Getenv("RMQ_USERNAME")
var rmqPassword = os.Getenv("RMQ_PASSWORD")
var rmqExchange = os.Getenv("RMQ_EXCHANGE")
var rmqExchangeType = os.Getenv("RMQ_EXCHANGE_TYPE")
var rmqQueue = os.Getenv("RMQ_QUEUE")
var rmqRoutingKey = os.Getenv("RMQ_ROUTING_KEY")

var deadConns = make(chan net.Conn, 128)

var RMQURL = "amqp://" + rmqUser + ":" + rmqPassword + "@" + rmqHost + ":" + rmqPort
var MyTLV = []byte{0, 16, 0, 4, 0, 0, 0, 0} //STATEFUL-PCE-CAPABILITY TLV with all capabilities unset

var allReportedLSP = make(map[string]bool)
var DataLocker = &sync.RWMutex{}

const pcepConnType = "tcp"
const PCEPVersionAndFlags = 32 //version1 and all flags unset
const PCEPKeepaliveFrequency = 30
const PCEPDeadTime = PCEPKeepaliveFrequency * 4
const PCEPsID = 0 // PCEP Session ID
const objectHeaderValuesLength = 4

var (
	goRoutines = prometheus.NewGaugeVec(
		prometheus.GaugeOpts{
			Name: "goRoutinesStatus",
			Help: "goRoutines counter for each task",
		},
		[]string{"goRoutine"},
	)
	sumReportedLSP = prometheus.NewCounter(
		prometheus.CounterOpts{
			Name: "sumReportedLSP",
			Help: "Sum of LSPs reported by the clients",
		},
	)
	currentReportedLSPs = prometheus.NewGauge(
		prometheus.GaugeOpts{
			Name: "currentReportedLSPs",
			Help: "Count of current LSPs reported by the clients",
		},
	)
)

func init() {
	// Metrics have to be registered to be exposed:
	prometheus.MustRegister(goRoutines)
	prometheus.MustRegister(sumReportedLSP)
	prometheus.MustRegister(currentReportedLSPs)

	/////////////////////////// Getting environment variables ////////////////////////////////

	if len(pcepBindingAddress) == 0 {
		pcepBindingAddress = "127.0.0.1"
	}
	if len(pcepBindingPort) == 0 {
		pcepBindingPort = "4189"
	}
	if len(rmqHost) == 0 {
		rmqHost = "10.255.45.111"
	}
	if len(rmqPort) == 0 {
		rmqPort = "31672"
	}
	if len(rmqUser) == 0 {
		rmqUser = "admin"
	}
	if len(rmqPassword) == 0 {
		rmqPassword = "password"
	}
	if len(rmqExchange) == 0 {
		rmqExchange = "shift_topology_exchange"
	}
	if len(rmqExchangeType) == 0 {
		rmqExchangeType = "topic"
	}
	if len(rmqQueue) == 0 {
		rmqQueue = "shift_lsp_topology_queue"
	}
	if len(rmqRoutingKey) == 0 {
		rmqRoutingKey = "shift_lsp_topology_key"
	}
}

func main() {

	go func() {
		// create a new mux server
		server := http.NewServeMux()
		// register a new handler for the /metrics endpoint
		server.Handle("/metrics", promhttp.Handler())
		// start an http server using the mux server
		http.ListenAndServe(":60000", server)
	}()

	// LSP data publish
	publisherChannel := make(chan []byte)
	go rmqPublisher(rmqExchange, rmqExchangeType, rmqQueue, rmqRoutingKey, publisherChannel)

	// PCRpt preprocessing
	preProcessingChannel := make(chan lspData)
	go pcrptPreProcessing(preProcessingChannel, publisherChannel)

	go connectionCloser(deadConns)

	// Listen for incoming connections.
	l, err := net.Listen(pcepConnType, pcepBindingAddress+":"+pcepBindingPort)
	if err != nil {
		fmt.Println("Error listening:", err.Error())
		os.Exit(1)
	}
	// Close the listener when the application closes.
	defer l.Close()
	fmt.Println("Listening on " + pcepBindingAddress + ":" + pcepBindingPort)
	for {
		// Listen for an incoming connection.
		conn, err := l.Accept()
		if err != nil {
			fmt.Println("Error accepting: ", err.Error())
			os.Exit(1)
		}
		// Handle connections in a new goroutine.
		go newConnection(conn, preProcessingChannel)
	}
}

// Handles new connections.
func newConnection(conn net.Conn, preProcessingChannel chan lspData) {

	pccIP, pccPort, _ := net.SplitHostPort(conn.RemoteAddr().String())

	goRoutines.WithLabelValues("New connection").Inc()
	defer goRoutines.WithLabelValues("New connection").Dec()

	messagesReceived := make(chan []byte)
	defer close(messagesReceived)

	clientWriter := bufio.NewWriter(conn)
	clientReader := bufio.NewReader(conn)

	go messagesHandler(pccIP, pccPort, clientWriter, messagesReceived, conn, preProcessingChannel)

	// go func() {

	newPCEPMessage := make([]byte, 0, 4)
	var pcepMessageLength uint16
	var low, high int
	low = 0
	high = 0
	var reassembling bool

	for {
		// Make a buffer to hold incoming data.
		buf := make([]byte, 4096)

		// Read the incoming connection into the buffer.
		nbyte, err := clientReader.Read(buf)
		if err != nil {
			log.Printf("Connection to %v closed\n", conn.RemoteAddr())
			deadConns <- conn
			break
		} else {

			// copy content in buffer and delimits to actual data size
			fragment := make([]byte, nbyte)
			copy(fragment, buf[:nbyte])

			for {
				if !reassembling {
					commonHeader := fragment[low : low+4]
					pcepMessageLength = binary.BigEndian.Uint16(commonHeader[2:])
					high = low + int(pcepMessageLength)
					newPCEPMessage = make([]byte, 0, 4)
				}
				if high == len(fragment) {
					newPCEPMessage = append(newPCEPMessage, fragment[low:high]...)
					messagesReceived <- newPCEPMessage
					reassembling = false
					low = 0
					high = 0
					break
				} else if high < len(fragment) {
					newPCEPMessage = append(newPCEPMessage, fragment[low:high]...)
					messagesReceived <- newPCEPMessage
					reassembling = false
					low = high
				} else if high > len(fragment) {
					newPCEPMessage = append(newPCEPMessage, fragment[low:]...)
					low = 0
					high = high - len(fragment)
					reassembling = true
					break
				}
			}
		}
	}
	// }()
}

func messagesHandler(pccIP, pccPort string, clientWriter *bufio.Writer, messagesReceived chan []byte, conn net.Conn, preProcessingChannel chan lspData) {

	goRoutines.WithLabelValues("Message handler").Inc()
	defer goRoutines.WithLabelValues("Message handler").Dec()

	keepaliveSenderChannel := make(chan bool)
	defer keepaliveSenderChannelSend(keepaliveSenderChannel)

	keepalivesCounter := 0

	for msg := range messagesReceived {

		var commonObjectHeader, lspObject, lspObjectTLV, bandwidthObject, rroObject []byte
		var lspObjectHeaderValues, plspID uint32
		var pcrptMessageLength, objectLength, lspObjectTLVType, lspObjectTLVLength uint16
		var low, high, tlvLow, tlvHigh, padding, subObjectLow int
		var rroSubObjectType, rroSubObjectLength byte
		var lspDelegate, lspSync, lspRemove, lspAdministrative, lspOperational bool

		commonHeader := msg[:4]

		/**
		==> PCEP Common Header

			0               1               2               3
			0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
			+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			| Ver |  Flags  |  Message-Type |       Message-Length        |
			+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

			Message-Type (8 bits):  The following message types are currently
			defined:

				Value    Meaning
				1        Open
				2        Keepalive
				3        Path Computation Request
				4        Path Computation Reply
				5        Notification
				6        Error
				7        Close
				10       Report
				11       Update
		**/
		switch {

		//PCEP open message
		case commonHeader[1] == 1:
			/**
			==> PCEP OPEN Object

				0               1               2               3
				0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|Ver|   Flags   |   Keepalive   |  DeadTimer    |      SID    |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|                                                             |
				//                       Optional TLVs                       //
				|                                                             |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

				Optional TLV(s):

				0               1               2               3
				0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|               Type=[TBD]      |            Length=4         |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|                                                             |
				//                         TLV Content                       //
				|                                                             |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
			**/
			var myOpenMessage []byte
			var myOpenObject []byte

			fmt.Println(msg)

			log.Println("PCEP Open message received")
			log.Printf("PCEP Version: %v\n", (commonHeader[0]&0xE0)>>5)
			log.Printf("PCEP flags: %v\n", commonHeader[0]&0x1F)

			commonObjectHeader = msg[4:8]
			log.Println("PCEP Open object read")
			log.Printf("PCEP OPEN => Object Class: %v\n", (commonObjectHeader[0]))
			log.Printf("PCEP OPEN => Object Type: %v\n", (commonObjectHeader[1] >> 4))
			log.Printf("PCEP OPEN => Reserved flags: %v\n", (commonObjectHeader[1] & 0xC >> 2))
			log.Printf("PCEP OPEN => P flag (Processing Rule): %v\n", (commonObjectHeader[1] & 0x2 >> 1))
			openObjectLength := binary.BigEndian.Uint16(commonObjectHeader[2:])
			log.Printf("PCEP OPEN => Object Length: %v\n", openObjectLength)

			if (commonObjectHeader[0] == 1) && (commonObjectHeader[1]>>4 == 1) { // OPEN Object-Class is 1. OPEN Object-Type is 1
				openObject := msg[8:12]
				log.Printf("PCEP OPEN => PCEP version: %v\n", (openObject[0] >> 5))
				log.Printf("PCEP OPEN => Reserved flags: %v\n", (openObject[0] & 0x1F))
				log.Printf("PCEP OPEN => Keepalive frequency: %v\n", (openObject[1]))
				log.Printf("PCEP OPEN => DeadTimer: %v\n", (openObject[2]))
				log.Printf("PCEP OPEN => SID: %v\n", (openObject[3]))

				if int(openObjectLength) > 12 { // 12 would be the open object length with no optional TLV
					log.Println("PCEP Open object contains optional TLVs")
					optionalTLV := msg[12:]
					tlvType := binary.BigEndian.Uint16(optionalTLV[0:])
					tlvLength := binary.BigEndian.Uint16(optionalTLV[2:])
					log.Printf("PCEP OPEN => Optional TLV type: %v\n", tlvType)
					log.Printf("PCEP OPEN => Optional TLV length: %v\n", tlvLength)
					if tlvType == 16 {
						statefulPCECapability := binary.BigEndian.Uint32(optionalTLV[4:])
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => LSP-UPDATE-CAPABILITY (U) => %v\n", statefulPCECapability&0x01)
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => INCLUDE-DB-VERSION (S) => %v\n", statefulPCECapability&0x02>>1)
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => LSP-INSTANTIATION-CAPABILITY (I) => %v\n", statefulPCECapability&0x04>>2)
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => TRIGGERED-RESYNC (T) => %v\n", statefulPCECapability&0x08>>3)
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => DELTA-LSP-SYNC-CAPABILITY (D) => %v\n", statefulPCECapability&0x16>>4)
						log.Printf("PCEP OPEN => STATEFUL-PCE-CAPABILITY => TRIGGERED-INITIAL-SYNC (F) => %v\n", statefulPCECapability&0x32>>5)
					}
				}
			}

			//building our PCEP OPEN message
			myOpenMessage = append(myOpenMessage, commonHeader...)       //adding common PCEP Header
			myOpenMessage = append(myOpenMessage, commonObjectHeader...) // adding common object header

			myOpenObject = append(myOpenObject, byte(PCEPVersionAndFlags))
			myOpenObject = append(myOpenObject, byte(PCEPKeepaliveFrequency))
			myOpenObject = append(myOpenObject, byte(PCEPDeadTime))
			myOpenObject = append(myOpenObject, byte(PCEPsID))

			myOpenMessage = append(myOpenMessage, myOpenObject...) // adding custom OPEN object
			myOpenMessage = append(myOpenMessage, MyTLV...)        // adding custom TLV

			// Send a open message back
			conn.Write([]byte(myOpenMessage))

		//PCEP keepalive
		case commonHeader[1] == 2:
			log.Printf("PCEP Keepalive received from %v:%v\n", pccIP, pccPort)
			if keepalivesCounter < 1 {
				//Once we receive the first keealive, a new goroutine is initiated to send keepalives periodically
				keepalivesCounter++
				go keepaliveSender(clientWriter, msg, pccIP, pccPort, keepaliveSenderChannel)
			}

		// PCEP Close
		case commonHeader[1] == 7:
			/**
			--> PCEP Close object
			    0                   1                   2                   3
				0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|          Reserved             |      Flags    |    Reason     |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				|                                                               |
				//                         Optional TLVs                       //
				|                                                               |
				+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

				Reasons
					Value      Meaning
					1          No explanation provided
					2          DeadTimer expired
					3          Reception of a malformed PCEP message
					4          Reception of an unacceptable number of unknown
								requests/replies
					5          Reception of an unacceptable number of unrecognized
								PCEP messages
			**/

			log.Printf("Received CLOSE message => Reason: %v\n", msg[7])
			log.Printf("Session with %v:%v closed\n", pccIP, pccPort)
			deadConns <- conn

		// PCRpt
		case commonHeader[1] == 10:

			log.Println("PCRpt received")
			pcrptMessageLength = binary.BigEndian.Uint16(commonHeader[2:])
			log.Printf("PCRpt message length: %v\n", pcrptMessageLength)
			low = 4
			high = 8
			var lspReport lspData
			for {
				commonObjectHeader = msg[low:high]
				/**
				==> PCEP Common Object Header

					0               1               2               3
					0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					| Object-Class  |   OT  |Res|P|I|   Object Length (bytes)       |
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					|                                                               |
					//                        (Object body)                        //
					|                                                               |
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
				**/
				log.Printf("PCRpt => Object Class: %v\n", (commonObjectHeader[0]))
				log.Printf("PCRpt => Object Type: %v\n", (commonObjectHeader[1] >> 4))
				log.Printf("PCRpt => Reserved flags: %v\n", (commonObjectHeader[1] & 0xC >> 2))
				log.Printf("PCRpt => P flag (Processing Rule): %v\n", (commonObjectHeader[1] & 0x2 >> 1))
				log.Printf("PCRpt => I flag (Processing Rule): %v\n", (commonObjectHeader[1] & 0x1))
				objectLength = binary.BigEndian.Uint16(commonObjectHeader[2:])
				log.Printf("PCRpt => Object Length: %v\n", objectLength)

				switch {
				case commonObjectHeader[0] == 32:
					/**
					==> PCRpt LSP Object

						0               1               2               3
						0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
						+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
						|                PLSP-ID                | Flags |C|  O|A|R|S|D|
						+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
						//                        TLVs                               //
						|                                                             |
						+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
						   O (Operational - 3 bits):  the operational status of the LSP.
													The following values are defined:
													0 - DOWN:         not active.
													1 - UP:           signaled.
													2 - ACTIVE:       up and carrying traffic.
													3 - GOING-DOWN:   LSP is being torn down, and resources are being released.
													4 - GOING-UP:     LSP is being signaled.
													5-7 - Reserved:   these values are reserved for future use.
					**/
					sumReportedLSP.Inc()

					if lspReport.LSP.Symbolic_path_name != "" {
						preProcessingChannel <- lspReport
						if lspReport.LSP.Flags.Remove {
							DataLocker.Lock()
							delete(allReportedLSP, lspReport.LSP.Symbolic_path_name)
							currentReportedLSPs.Set(float64(len(allReportedLSP)))
							DataLocker.Unlock()
						}
						fmt.Println("----------------------------------------------------")
					}
					lspReport = lspData{}

					lspReport.LSP.PCC = pccIP
					lspObject = msg[high : high+int(objectLength)-4]
					lspObjectHeaderValues = binary.BigEndian.Uint32(lspObject)

					plspID = lspObjectHeaderValues >> 12 & 0xFFFFF
					if int(lspObjectHeaderValues&0x1) > 0 {
						lspDelegate = true
					}
					if int(lspObjectHeaderValues&0x2>>1) > 0 {
						lspSync = true
					}
					if int(lspObjectHeaderValues&0x4>>2) > 0 {
						lspRemove = true
					}
					if int(lspObjectHeaderValues&0x8>>3) > 0 {
						lspAdministrative = true
					}
					if 0 < int(lspObjectHeaderValues&0x70>>4) && int(lspObjectHeaderValues&0x70>>4) < 3 {
						lspOperational = true
					}
					log.Printf("PCRpt => LSP Object => PLSP-ID: %v\n", plspID)
					log.Printf("PCRpt => LSP Object => Delegate: %v\n", lspDelegate)
					log.Printf("PCRpt => LSP Object => Sync: %v\n", lspSync)
					log.Printf("PCRpt => LSP Object => Remove: %v\n", lspRemove)
					log.Printf("PCRpt => LSP Object => Administrative: %v\n", lspAdministrative)
					log.Printf("PCRpt => LSP Object => Operational: %v\n", lspOperational)
					log.Printf("PCRpt => LSP Object => Reserved flags: %v\n", lspObjectHeaderValues&0xF80>>7)

					lspReport.LSP.Plsp_id = plspID
					lspReport.LSP.Flags.Delegate = lspDelegate
					lspReport.LSP.Flags.Sync = lspSync
					lspReport.LSP.Flags.Remove = lspRemove
					lspReport.LSP.Flags.Administrative = lspAdministrative
					lspReport.LSP.Flags.Operational = lspOperational

					tlvLow = 4
					tlvHigh = int(objectLength) - objectHeaderValuesLength

					for {
						lspObjectTLV = lspObject[tlvLow:]
						lspObjectTLVType = binary.BigEndian.Uint16(lspObjectTLV)
						lspObjectTLVLength = binary.BigEndian.Uint16(lspObjectTLV[2:])
						padding = 0
						log.Printf("PCRpt => LSP Object => TLV type: %v\n", lspObjectTLVType)
						log.Printf("PCRpt => LSP Object => TLV length: %v\n", lspObjectTLVLength)

						switch {
						case lspObjectTLVType == 17:
							/**
							==> PCRpt LSP SYMBOLIC-PATH-NAME TLV
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|           Type=17             |       Length (variable)       |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                                                               |
								//                      Symbolic Path Name                     //
								|                                                               |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							**/
							lspReport.LSP.Symbolic_path_name = string(lspObjectTLV[4 : 4+int(lspObjectTLVLength)])
							log.Printf("PCRpt => LSP Object => SYMBOLIC-PATH-NAME: %v\n", string(lspObjectTLV[4:4+int(lspObjectTLVLength)]))
							DataLocker.Lock()
							allReportedLSP[lspReport.LSP.Symbolic_path_name] = true
							currentReportedLSPs.Set(float64(len(allReportedLSP)))
							DataLocker.Unlock()

							if int(lspObjectTLVLength%4) > 0 {
								padding = 4 - int(lspObjectTLVLength%4)
							}
						case lspObjectTLVType == 18:
							/**
							==> PCRpt LSP IPv4-LSP-IDENTIFIERS TLV
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|           Type=18             |           Length=16           |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                   IPv4 Tunnel Sender Address                  |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|             LSP ID            |           Tunnel ID           |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                        Extended Tunnel ID                     |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                   IPv4 Tunnel Endpoint Address                |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							**/
							lspReport.LSP.Ipv4_tunnel_sender_address = net.IP(lspObjectTLV[4:8])
							lspReport.LSP.Lsp_id = binary.BigEndian.Uint16(lspObjectTLV[8:10])
							lspReport.LSP.Tunnel_id = binary.BigEndian.Uint16(lspObjectTLV[10:12])
							lspReport.LSP.Extended_tunnel_id = binary.BigEndian.Uint32(lspObjectTLV[12:16])
							lspReport.LSP.Ipv4_tunnel_endpoint_address = net.IP(lspObjectTLV[16:20])
							log.Printf("PCRpt => LSP Object => IPV4-LSP-IDENTIFIERS => IPv4-tunnel-sender-address: %v\n", net.IP(lspObjectTLV[4:8]))
							log.Printf("PCRpt => LSP Object => IPV4-LSP-IDENTIFIERS => LSP-ID: %v\n", binary.BigEndian.Uint16(lspObjectTLV[8:10]))
							log.Printf("PCRpt => LSP Object => IPV4-LSP-IDENTIFIERS => Tunnel-ID: %v\n", binary.BigEndian.Uint16(lspObjectTLV[10:12]))
							log.Printf("PCRpt => LSP Object => IPV4-LSP-IDENTIFIERS => Extended-tunnel-ID: %v\n", binary.BigEndian.Uint32(lspObjectTLV[12:16]))
							log.Printf("PCRpt => LSP Object => IPV4-LSP-IDENTIFIERS => IPv4-tunnel-endpoint-address: %v\n", net.IP(lspObjectTLV[16:20]))
						case lspObjectTLVType == 20:
							/**
							==> PCRpt LSP LSP-ERROR-CODE TLV
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|           Type=20             |            Length=4           |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                          LSP Error Code                       |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

									Value      Meaning
									---       -------------------------------------
									0        Reserved
									1        Unknown reason
									2        Limit reached for PCE-controlled LSPs
									3        Too many pending LSP Update Requests
									4        Unacceptable parameters
									5        Internal error
									6        LSP administratively brought down
									7        LSP preempted
									8        RSVP signaling error
							**/
							log.Printf("PCRpt => LSP Object => LSP-ERROR-CODE: %v\n", binary.BigEndian.Uint32(lspObjectTLV[4:8]))
						case lspObjectTLVType == 21:
							/**
							==> PCRpt LSP RSVP-ERROR-SPEC TLV
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|           Type=21             |            Length (variable)  |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                                                               |
								+                RSVP ERROR_SPEC or USER_ERROR_SPEC Object      +
								|                                                               |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							**/
							log.Printf("PCRpt => LSP Object => RSVP-ERROR-SPEC: %v\n", string(lspObjectTLV[4:4+int(lspObjectTLVLength)]))
							if int(lspObjectTLVLength%4) > 0 {
								padding = 4 - int(lspObjectTLVLength%4)
							}
						case lspObjectTLVType == 19:
							/**
							==> PCRpt LSP IPv6-LSP-IDENTIFIERS TLV
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|           Type=19             |           Length=52           |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                                                               |
								+                                                               +
								|                  IPv6 Tunnel Sender Address                   |
								+                          (16 octets)                          +
								|                                                               |
								+                                                               +
								|                                                               |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|             LSP ID            |           Tunnel ID           |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                                                               |
								+                                                               +
								|                       Extended Tunnel ID                      |
								+                          (16 octets)                          +
								|                                                               |
								+                                                               +
								|                                                               |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
								|                                                               |
								+                                                               +
								|                  IPv6 Tunnel Endpoint Address                 |
								+                          (16 octets)                          +
								|                                                               |
								+                                                               +
								|                                                               |
								+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							**/
							log.Printf("PCRpt => LSP Object => IPV6-LSP-IDENTIFIERS (NOT IMPLEMENTED)\n")
						}

						tlvLow = tlvLow + int(lspObjectTLVLength) + int(padding) + 4
						if tlvLow >= tlvHigh {
							break
						}
					}
				case commonObjectHeader[0] == 5:
					/**
					==> PCRpt BANDWIDTH Object
						0                   1                   2                   3
						0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					|                        Bandwidth                              |
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					**/
					bandwidthObject = msg[high : high+int(objectLength)-4]
					lspReport.Bandwidth = math.Float32frombits(binary.BigEndian.Uint32(bandwidthObject))
					log.Printf("PCRpt => BANDWIDTH Object => Bandwidth: %v Bps\n", math.Float32frombits(binary.BigEndian.Uint32(bandwidthObject)))

				case commonObjectHeader[0] == 8:
					/**
					==> PCRpt RECORD ROUTE Object
						0                   1                   2                   3
						0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					|                                                               |
					//                        (Subobjects)                          //
					|                                                               |
					+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
					**/
					rroObject = msg[high : high+int(objectLength)-4]
					subObjectLow = 0
					for {
						rroSubObjectType = rroObject[subObjectLow]
						rroSubObjectLength = rroObject[subObjectLow+1]
						log.Printf("PCRpt => RECORD ROUTE Object => Type: %v\n", rroSubObjectType)

						switch {
						case rroSubObjectType == 1:
							/**
							==> IPv4 Address Subobject
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							|      Type     |     Length    | IPv4 address (4 bytes)        |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							| IPv4 address (continued)      | Prefix Length |      Flags    |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

								Flags:	Value      Meaning
										---       -------------------------------------
										0x01       Local protection available
										0x02       Local protection in use

							**/
							var subrro rro
							subrro.Address = net.IP(rroObject[subObjectLow+2 : subObjectLow+6])
							subrro.Prefix_length = rroObject[subObjectLow+6]
							if int(rroObject[subObjectLow+7]&0x1) > 0 {
								subrro.Local_protection_available = true
							}
							if int(rroObject[subObjectLow+7]&0x2>>1) > 0 {
								subrro.Local_protection_in_use = true
							}
							lspReport.RRO = append(lspReport.RRO, subrro)

							log.Printf("PCRpt => RECORD ROUTE Object => IPv4 Address => IPv4 Address: %v\n", net.IP(rroObject[subObjectLow+2:subObjectLow+6]))
							log.Printf("PCRpt => RECORD ROUTE Object => IPv4 Address => Prefix length: %v\n", rroObject[subObjectLow+6])
							log.Printf("PCRpt => RECORD ROUTE Object => IPv4 Address => Flags: %v\n", rroObject[subObjectLow+7])
						case rroSubObjectType == 2:
							/**
							==> IPv6 Address Subobject
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							|      Type     |     Length    | IPv6 address (16 bytes)       |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							| IPv6 address (continued)                                      |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							| IPv6 address (continued)                                      |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							| IPv6 address (continued)                                      |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							| IPv6 address (continued)      | Prefix Length |      Flags    |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

								Flags:	Value      Meaning
										---       -------------------------------------
										0x01       Local protection available
										0x02       Local protection in use
							**/
							log.Printf("PCRpt => RECORD ROUTE Object => IPv6 Address ==> IPv6 Address: %v\n", net.IP(rroObject[subObjectLow+2:subObjectLow+18]))
							log.Printf("PCRpt => RECORD ROUTE Object => IPv6 Address => Prefix length: %v\n", rroObject[subObjectLow+18])
							log.Printf("PCRpt => RECORD ROUTE Object => IPv6 Address => Flags: %v\n", rroObject[subObjectLow+19])
						case rroSubObjectType == 3:
							/**
							==> Label Subobject
								0                   1                   2                   3
								0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							|     Type      |     Length    |    Flags      |   C-Type      |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
							|       Contents of Label Object                                |
							+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

								Flags:	Value      Meaning
										---       -------------------------------------
										0x01       Global label
							**/
							log.Printf("PCRpt => RECORD ROUTE Object => Label ==> Flags: %v\n", rroObject[subObjectLow+2])
							log.Printf("PCRpt => RECORD ROUTE Object => Label ==> C-Type: %v\n", rroObject[subObjectLow+3])
							log.Printf("PCRpt => RECORD ROUTE Object => Label ==> Label: %v\n", binary.BigEndian.Uint32(rroObject[subObjectLow+4:subObjectLow+int(rroSubObjectLength)]))
						}
						subObjectLow = subObjectLow + int(rroSubObjectLength)
						if subObjectLow >= int(objectLength)-objectHeaderValuesLength {
							break
						}
					}
				default:
					log.Printf("PCRpt Object %v not implemented or undefined\n", commonObjectHeader[0])
				}

				low = low + int(objectLength)
				high = high + int(objectLength)
				if high > int(pcrptMessageLength) {
					preProcessingChannel <- lspReport
					if lspReport.LSP.Flags.Remove {
						DataLocker.Lock()
						delete(allReportedLSP, lspReport.LSP.Symbolic_path_name)
						currentReportedLSPs.Set(float64(len(allReportedLSP)))
						DataLocker.Unlock()
					}
					fmt.Println("----------------------------------------------------")
					break
				}
			}
		}
	}
}

// keepaliveSender sends keepalives each 30 seconds
func keepaliveSender(clientWriter *bufio.Writer, keepalive []byte, pccIP, pccPort string, keepaliveSenderChannel chan bool) {

	goRoutines.WithLabelValues("Keepalive sender").Inc()
	defer goRoutines.WithLabelValues("Keepalive sender").Dec()
	defer close(keepaliveSenderChannel)

	for {
		select {
		case <-keepaliveSenderChannel:
			log.Printf("Keepalive goroutine to %v:%v 'stopped'\n", pccIP, pccPort)
			return
		default:
			clientWriter.Write(keepalive)
			clientWriter.Flush()
			log.Printf("PCEP Keepalive sent to %v:%v\n", pccIP, pccPort)
			time.Sleep(30 * time.Second)
		}
	}
}

// pcrptPreProcessing converts structs into json
func pcrptPreProcessing(preProcessingChannel chan lspData, publisherChannel chan []byte) {
	goRoutines.WithLabelValues("PCRpt preprocessing").Set(1)
	defer goRoutines.WithLabelValues("PCRpt preprocessing").Set(0)
	defer close(preProcessingChannel)

	for report := range preProcessingChannel {
		data, err := json.Marshal(report)
		if err == nil {
			publisherChannel <- data
		} else {
			log.Printf("ERROR while preprocessing lsp data: %v\n", err)
		}
	}
}

func rmqPublisher(rmqExchange, rmqExchangeType, rmqQueue, rmqRoutingKey string, publisherChannel chan []byte) {

	conn, err := amqp.Dial(RMQURL)
	failOnError(err, "Failed to connect to RabbitMQ")
	defer conn.Close()

	ch, err := conn.Channel()
	failOnError(err, "Failed to open a channel")

	err = ch.ExchangeDeclare(
		rmqExchange,
		rmqExchangeType,
		false,
		false,
		false,
		false,
		nil,
	)
	failOnError(err, "Failed to create exchange")

	q, err := ch.QueueDeclare(
		rmqQueue, // name
		false,    // durable
		false,    // delete when unused
		false,    // exclusive
		false,    // no-wait
		amqp.Table{"x-ha-policy": "all"}, // arguments
	)
	failOnError(err, "Failed to declare a queue")

	err = ch.QueueBind(
		q.Name,
		rmqRoutingKey,
		rmqExchange,
		false,
		nil,
	)
	failOnError(err, "Failed to bind")

	defer ch.Close()

	goRoutines.WithLabelValues("RMQConnection").Set(1)
	defer goRoutines.WithLabelValues("RMQConnection").Set(0)
	defer close(publisherChannel)

	for message := range publisherChannel {
		err = ch.Publish(
			rmqExchange,   // exchange
			rmqRoutingKey, // routing key
			false,         // mandatory
			false,         // immediate
			amqp.Publishing{
				ContentType: "text/plain",
				Body:        message,
			})
		failOnError(err, "Failed to publish a message")
	}
}

func connectionCloser(deadConns chan net.Conn) {
	goRoutines.WithLabelValues("connectionCloser").Set(1)
	defer goRoutines.WithLabelValues("connectionCloser").Set(0)

	for conn := range deadConns {
		conn.Close()
	}
}

func failOnError(err error, msg string) {
	if err != nil {
		log.Fatalf("%s: %s", msg, err)
		// panic(fmt.Sprintf("%s: %s", msg, err))
	}
}

func keepaliveSenderChannelSend(keepaliveSenderChannel chan bool) {
	keepaliveSenderChannel <- true
}
