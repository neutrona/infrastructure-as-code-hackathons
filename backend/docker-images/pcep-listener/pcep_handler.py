#!/usr/bin/python3

import threading
from time import sleep
import struct
import ipaddress
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

"""

 Ref.:

 Path Computation Element (PCE) Communication Protocol (PCEP)
 https://tools.ietf.org/html/rfc5440 

 Path Computation Element Communication Protocol (PCEP)
 Extensions for Stateful PCE
 https://tools.ietf.org/html/rfc8231

 RSVP-TE: Extensions to RSVP for LSP Tunnels
 https://tools.ietf.org/html/rfc3209

 struct — Interpret bytes as packed binary data
 https://docs.python.org/3/library/struct.html 

"""


class PCEP_KEEPALIVE(threading.Thread):
    def __init__(self, client_socket, stop_event, interval=30):
        threading.Thread.__init__(self)
        self.client_socket = client_socket
        self.stop_event = stop_event
        self.interval = interval

    def sendKeepalive(self):
        # Send PCEP Keepalive Message
        logger.info("Sending Keepalive Message to PCC (Keepalive Thread)")
        # Begin packing...
        r_bytes = bytearray(4)
        r_offset = 0

        # Keepalive Message, len=4
        struct.pack_into("!BBH", r_bytes, r_offset, 32, 2, 4)

        self.client_socket.sendall(r_bytes)
        logger.info("Keepalive Message to PCC sent (Keepalive Thread)")

    def run(self):
        while not self.stop_event.is_set():
            self.sendKeepalive()
            sleep(self.interval)


class PCEP(object):
    def on_pcrpt(self, pcrpt):
        pass

    def __init__(self, client_socket, stop_event, on_pcrpt_callback=on_pcrpt):
        self.client_socket = client_socket
        self.on_pcrpt = on_pcrpt_callback
        self.stop_event = stop_event

    def sendKeepalive(self):
        # Send PCEP Keepalive Message
        logger.info("Sending Keepalive Message to PCC")
        # Begin packing...
        r_bytes = bytearray(4)
        r_offset = 0

        # Keepalive Message, len=4
        struct.pack_into("!BBH", r_bytes, r_offset, 32, 2, 4)

        self.client_socket.sendall(r_bytes)
        logger.info("Keepalive Message to PCC sent")

    def run(self):
        # Read from socket
        data = bytes(self.client_socket.recv(512))

        # Exit loop if no more data available (socket close)
        if len(data) == 0:
            return False

        cur_thread = threading.current_thread()
        logger.info('{}: {}.len: {}'.format(cur_thread.name, data, len(data)))

        # Begin PCEP processing...
        """ PCEP Common Header

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
                  10        Report
                  11        Update
        """

        # Begin unpacking...

        # PCEP Common Header

        """
            '!BBH' = byte order:    network (= big-endian),
                     byte0:         unsigned char (integer),
                     byte1:         unsigned char (integer),
                     byte2-3:       unsigned short (integer)
        """

        # Set up some fixed lengths
        common_hdr_len = 4
        common_obj_hdr_len = 4
        open_obj_hdr_len = 4
        tlv_hdr_len = 4

        # Global offset and expected PCEP messages length
        message_length_sum = 0
        offset = 0

        # Continue processing messages until the end of received data
        while offset != len(data):

            # Unpack PCEP common header
            common_hdr = struct.unpack('!BBH',
                                       data[offset:offset + common_hdr_len])

            # Compute offset and expected PCEP messages length
            offset = offset + common_hdr_len
            message_length = common_hdr[2]

            message_length_sum = message_length_sum + message_length

            # Find out if there's segmentation and attempt to retrieve and reassemble missing data
            while message_length_sum > len(data):
                data = data + bytes(self.client_socket.recv(4096))
                logger.info('---Message Length Sum: %i. Reassembled data length: %i' % (message_length_sum, len(data)))

            # Continue processing PCEP common header
            """
                The three most significant bits in the most significant byte in
                the PCEP common header indicate the PCEP version. 
                Extract by shifting 5 bits to the right, padding with '0'.

                The five least significant bits in the same byte indicate the
                PCEP Flags.
                Extract by applying a binary mask of 0b00011111.                
            """
            logger.info('PCEP Version: %i' % (common_hdr[0] >> 5))

            logger.info('PCEP Flags: %s' % (common_hdr[0] & 0b00011111))

            logger.info('PCEP Message Type: %s' % (common_hdr[1]))

            logger.info('PCEP Message Length: %i ' % message_length)

            # Begin processing PCEP messages

            # PCEP Keepalive Message (Message-Type=2)
            if common_hdr[1] == 2:
                logger.info('PCEP Keepalive Message received')

                # Reply Keepalive Message
                self.sendKeepalive()

                logger.info('-----keepalive offset %i ' % offset)

            # PCEP Non-Keepalive Messages (Message-Type<>2)
            else:
                # Process message while computed local offset has not reached the end of the current message
                while offset - (message_length_sum - message_length) != message_length:
                    logger.info('----Global Offset: %i. Message Length: %i' % (offset, message_length))
                    # PCEP Open Message (Message-Type=1)
                    if common_hdr[1] == 1:
                        logger.info('PCEP Open Message received')

                        """ PCEP Common Object Header

                             0               1               2               3
                             0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            | Object-Class  |   OT  |Res|P|I|   Object Length (bytes)       |
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                            |                                                               |
                            //                        (Object body)                        //
                            |                                                               |
                            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                            '!BBH' = byte order:    network (= big-endian),
                                     byte0:         unsigned char (integer),
                                     byte1:         unsigned char (integer),
                                     byte2-3:       unsigned short (integer)
                        """

                        common_obj_hdr = struct.unpack('!BBH',
                                                       data[offset:offset + common_obj_hdr_len])

                        offset = offset + common_obj_hdr_len
                        obj_len = common_obj_hdr[2]

                        logger.info('PCEP Object Class: %s' % common_obj_hdr[0])
                        logger.info('PCEP Object Type: %s' % (common_obj_hdr[1] >> 4))
                        logger.info('PCEP Reserved Flags: %s' % (common_obj_hdr[1] >> 2 & 0b00000011))
                        logger.info('PCEP Processing-Rule (P): %s' % (common_obj_hdr[1] >> 1 & 0b00000001))
                        logger.info('PCEP Ignore (I): %s' % (common_obj_hdr[1] & 0b00000001))
                        logger.info('PCEP Object Length: %s' % obj_len)

                        # PCEP Open Object Class (Object-Class=1)
                        if common_obj_hdr[0] == 1:
                            # PCEP OPEN Object (Object-Type=1)
                            if common_obj_hdr[1] >> 4 == 1:
                                logger.info('PCEP OPEN Object received')

                                """ PCEP OPEN Object

                                   0               1               2               3
                                   0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                   |Ver|   Flags   |   Keepalive   |  DeadTimer    |      SID    |
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                   |                                                             |
                                   //                       Optional TLVs                       //
                                   |                                                             |
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                   '!BBBB' = byte order:    network (= big-endian),
                                             byte0:         unsigned char (integer),
                                             byte1:         unsigned char (integer),
                                             byte2:         unsigned char (integer),
                                             byte3:         unsigned char (integer)


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

                                   '!HH' = byte order:    network (= big-endian),
                                           byte0-1:         unsigned short (integer),
                                           byte2-3:         unsigned short (integer)

                                """

                                open_obj = struct.unpack('!BBBB',
                                                         data[offset: offset + open_obj_hdr_len])

                                offset = offset + open_obj_hdr_len

                                logger.info('PCEP OPEN:PCEP Version: %i' % (open_obj[0] >> 5))
                                logger.info('PCEP OPEN:Reserved Flags: %s' % (open_obj[0] & 0b00011111))
                                logger.info('PCEP OPEN:Keepalive: %i' % open_obj[1])
                                logger.info('PCEP OPEN:DeadTimer: %i' % open_obj[2])
                                logger.info('PCEP OPEN:Session Id (SID): %i' % open_obj[3])

                                # Check for OPEN Object Optional TLV(s)
                                opt_tlv_len = obj_len - common_obj_hdr_len - open_obj_hdr_len
                                while opt_tlv_len > 0:
                                    logger.info('PCEP OPEN: Found Optional TLVs')
                                    open_obj_opt_tlv_hdr = struct.unpack('!HH', data[offset: offset + tlv_hdr_len])

                                    offset = offset + tlv_hdr_len
                                    tlv_len = open_obj_opt_tlv_hdr[1]

                                    opt_tlv_len = opt_tlv_len - tlv_hdr_len - tlv_len

                                    logger.info('PCEP OPEN:Optional TLV Type: %i' % open_obj_opt_tlv_hdr[0])
                                    logger.info('PCEP OPEN:Optional TLV Length: %i ' % tlv_len)

                                    # OPEN Object STATEFUL-PCE-CAPABILITY TLV
                                    if open_obj_opt_tlv_hdr[0] == 16:
                                        stateful_tlv_capability_tlv = struct.unpack('!I',
                                                                                    data[offset: offset + tlv_len])

                                        offset = offset + tlv_len

                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:LSP-UPDATE-CAPABILITY (U) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000000001))
                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:INCLUDE-DB-VERSION (S) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000000010))
                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:LSP-INSTANTIATION-CAPABILITY (I) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000000100))
                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:TRIGGERED-RESYNC (T) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000001000))
                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:DELTA-LSP-SYNC-CAPABILITY (D) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000010000))
                                        logger.info(
                                            'PCEP OPEN:Optional TLV:STATEFUL-PCE-CAPABILITY:TRIGGERED-INITIAL-SYNC (F) %s' %
                                            (stateful_tlv_capability_tlv[0] & 0b00000000000000000000000000100000))

                                    # OPEN Object Unknown or Not Implemented TLV
                                    else:
                                        logger.info('PCEP OPEN:Optional TLV:Unknown or Not Implemented TLV')
                                        offset = offset + tlv_len

                                # Reply PCEP Open Message
                                logger.info("Sending Open Message to PCC")
                                # Begin packing...
                                r_bytes = bytearray(20)
                                r_offset = 0

                                # OPEN Message, len=20
                                struct.pack_into("!BBH", r_bytes, r_offset, 32, 1, 20)
                                r_offset = r_offset + common_hdr_len

                                # OPEN Object header, len=16
                                struct.pack_into("!BBH", r_bytes, r_offset, 1, 16, 16)
                                r_offset = r_offset + common_obj_hdr_len

                                # OPEN Object Keepalive: 30, Deadtime: 120, SID: 0
                                struct.pack_into('!BBBB', r_bytes, r_offset, 32, 30, 4 * 30, 0)
                                r_offset = r_offset + open_obj_hdr_len

                                """ STATEFUL-PCE-CAPABILITY TLV:
                                    .... .... .... .... .... .... .... ...0 = LSP-UPDATE-CAPABILITY (U): False
                                    .... .... .... .... .... .... .... ..0. = INCLUDE-DB-VERSION (S): False
                                    .... .... .... .... .... .... .... .0.. = LSP-INSTANTIATION-CAPABILITY (I): False
                                    .... .... .... .... .... .... .... 0... = TRIGGERED-RESYNC (T): False
                                    .... .... .... .... .... .... ...0 .... = DELTA-LSP-SYNC-CAPABILITY (D): False
                                    .... .... .... .... .... .... ..0. .... = TRIGGERED-INITIAL-SYNC (F): False
                                """
                                struct.pack_into('!HHI', r_bytes, r_offset, 16, 4, 0)

                                self.client_socket.sendall(r_bytes)
                                logger.info("Open Message to PCC sent")

                                logger.info('-----open offset %i' % offset)

                                # Start PCEP_KEEPALIVE thread
                                keepalive = PCEP_KEEPALIVE(client_socket=self.client_socket,
                                                           stop_event=self.stop_event,
                                                           interval=open_obj[1] / 2)
                                keepalive.start()

                    # PCEP Report Message (Message-Type=10)
                    elif common_hdr[1] == 10:
                        logger.info('PCEP Report Message received')

                        # PCRpt Dict (for JSON serialization)
                        pcrpt_dict = {}

                        # Process objects while computed local offset has not reached the end of the current message
                        while offset - (message_length_sum - message_length) != message_length:

                            common_obj_hdr = struct.unpack('!BBH',
                                                           data[offset:offset + common_obj_hdr_len])

                            offset = offset + common_obj_hdr_len
                            obj_len = common_obj_hdr[2]

                            logger.info('PCEP Object Class: %s' % common_obj_hdr[0])
                            logger.info('PCEP Object Type: %s' % (common_obj_hdr[1] >> 4))
                            logger.info('PCEP Reserved Flags: %s' % (common_obj_hdr[1] >> 2 & 0b00000011))
                            logger.info('PCEP Processing-Rule (P): %s' % (common_obj_hdr[1] >> 1 & 0b00000001))
                            logger.info('PCEP Ignore (I): %s' % (common_obj_hdr[1] & 0b00000001))
                            logger.info('PCEP Object Length: %i' % obj_len)

                            # PCRpt LSP Object
                            if common_obj_hdr[0] == 32:

                                # PCRpt callback (previous PCRpt in sequence)
                                if 'lsp' in pcrpt_dict:
                                    self.on_pcrpt(pcrpt=pcrpt_dict)
                                    pcrpt_dict = {}

                                logger.info('PCRpt LSP Object')

                                """
                                     0               1               2               3
                                     0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
                                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                     |                PLSP-ID                | Flags |C|  O|A|R|S|D|
                                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                     //                        TLVs                               //
                                     |                                                             |
                                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                     '!I' = byte order:      network (= big-endian),
                                            byte0-3:         unsigned int (integer)
                                """

                                lsp_obj_hdr_len = 4
                                lsp_obj = struct.unpack('!I', data[offset: offset + lsp_obj_hdr_len])
                                offset = offset + lsp_obj_hdr_len

                                # PCRpt.lsp Dict (for JSON serialization) ------------------------------------------- #
                                pcrpt_dict['lsp'] = {}

                                pcrpt_dict['lsp']['pcc'] = self.client_socket.getpeername()
                                logger.info('PCEP LSP:PCC: %s', pcrpt_dict['lsp']['pcc'])

                                pcrpt_dict['lsp']['flags'] = {}
                                pcrpt_dict['lsp']['plsp_id'] = \
                                    lsp_obj[0] >> 12 & 0b00000000000011111111111111111111
                                pcrpt_dict['lsp']['flags']['delegate'] = \
                                    bool(lsp_obj[0] & 0b00000000000000000000000000000001)
                                pcrpt_dict['lsp']['flags']['sync'] = \
                                    bool(lsp_obj[0] >> 1 & 0b00000000000000000000000000000001)
                                pcrpt_dict['lsp']['flags']['remove'] = \
                                    bool(lsp_obj[0] >> 2 & 0b00000000000000000000000000000001)
                                pcrpt_dict['lsp']['flags']['administrative'] = \
                                    bool(lsp_obj[0] >> 3 & 0b00000000000000000000000000000001)
                                pcrpt_dict['lsp']['flags']['operational'] = \
                                    bool(lsp_obj[0] >> 4 & 0b00000000000000000000000000000111)
                                # ----------------------------------------------------------------------------------- #

                                logger.info('PCEP PCRpt:LSP Object:PLSP-ID: %i' %
                                            (lsp_obj[0] >> 12 & 0b00000000000011111111111111111111))
                                logger.info('PCEP PCRpt:LSP Object:Delegate (D): %s' %
                                            (lsp_obj[0] & 0b00000000000000000000000000000001))
                                logger.info('PCEP PCRpt:LSP Object:SYNC (S): %s' %
                                            (lsp_obj[0] >> 1 & 0b00000000000000000000000000000001))
                                logger.info('PCEP PCRpt:LSP Object:Remove (R): %s' %
                                            (lsp_obj[0] >> 2 & 0b00000000000000000000000000000001))
                                logger.info('PCEP PCRpt:LSP Object:Administrative (A): %s' %
                                            (lsp_obj[0] >> 3 & 0b00000000000000000000000000000001))
                                logger.info('PCEP PCRpt:LSP Object:Operational (O): %s' %
                                            (lsp_obj[0] >> 4 & 0b00000000000000000000000000000111))
                                logger.info('PCEP PCRpt:LSP Object:Reserved Flags: %s' %
                                            (lsp_obj[0] >> 8 & 0b00000000000000000000000000001111))

                                # Check for PCRpt LSP Object Optional TLV(s)
                                opt_tlv_len = obj_len - common_obj_hdr_len - lsp_obj_hdr_len
                                while opt_tlv_len > 0:
                                    logger.info('PCEP PCRpt:LSP Object: Found TLVs')
                                    obj_opt_tlv_hdr = struct.unpack('!HH', data[offset: offset + tlv_hdr_len])

                                    offset = offset + tlv_hdr_len
                                    tlv_len = obj_opt_tlv_hdr[1]

                                    opt_tlv_len = opt_tlv_len - tlv_hdr_len - tlv_len

                                    logger.info('PCEP PCRpt:LSP Object:TLV Type: %s' % obj_opt_tlv_hdr[0])
                                    logger.info('PCEP PCRpt:LSP Object:TLV Length: %s', tlv_len)

                                    # PCRpt LSP Object SYMBOLIC-PATH-NAME TLV
                                    if obj_opt_tlv_hdr[0] == 17:
                                        symbolic_path_name_tlv = data[offset: offset + tlv_len]

                                        offset = offset + tlv_len

                                        # Compute and skip padding
                                        if tlv_len % 4 > 0:
                                            padding = 4 - tlv_len % 4
                                            logger.info('PADDING: %i' % padding)
                                            offset = offset + padding
                                            opt_tlv_len = opt_tlv_len - padding

                                        # PCRpt.lsp Dict (for JSON serialization) ----------------------------------- #
                                        pcrpt_dict['lsp']['symbolic_path_name'] = symbolic_path_name_tlv.decode('utf-8')
                                        # --------------------------------------------------------------------------- #

                                        logger.info('PCEP PCRpt:LSP Object:TLV:SYMBOLIC-PATH-NAME: %s' %
                                                    symbolic_path_name_tlv.decode('utf-8'))

                                    # PCRpt LSP Object IPV4-LSP-IDENTIFIERS
                                    elif obj_opt_tlv_hdr[0] == 18:
                                        ipv4_lsp_identifiers_tlv = struct.unpack('!IHHII',
                                                                                 data[offset: offset + tlv_len])

                                        offset = offset + tlv_len

                                        # PCRpt.lsp Dict (for JSON serialization) ----------------------------------- #
                                        pcrpt_dict['lsp']['ipv4_tunnel_sender_address'] = \
                                            str(ipaddress.IPv4Address(ipv4_lsp_identifiers_tlv[0]))
                                        pcrpt_dict['lsp']['lsp_id'] = ipv4_lsp_identifiers_tlv[1]
                                        pcrpt_dict['lsp']['tunnel_id'] = ipv4_lsp_identifiers_tlv[2]
                                        pcrpt_dict['lsp']['extended_tunnel_id'] = ipv4_lsp_identifiers_tlv[3]
                                        pcrpt_dict['lsp']['ipv4_tunnel_endpoint_address'] = \
                                            str(ipaddress.IPv4Address(ipv4_lsp_identifiers_tlv[4]))
                                        # --------------------------------------------------------------------------- #

                                        logger.info(
                                            'PCEP PCRpt:LSP Object:TLV:IPV4-LSP-IDENTIFIERS:IPv4 Tunnel Sender Address: %s' %
                                            (str(ipaddress.IPv4Address(ipv4_lsp_identifiers_tlv[0]))))
                                        logger.info('PCEP PCRpt:LSP Object:TLV:IPV4-LSP-IDENTIFIERS:LSP ID: %s' %
                                                    (ipv4_lsp_identifiers_tlv[1]))
                                        logger.info('PCEP PCRpt:LSP Object:TLV:IPV4-LSP-IDENTIFIERS:Tunnel ID: %s' %
                                                    (ipv4_lsp_identifiers_tlv[2]))
                                        logger.info(
                                            'PCEP PCRpt:LSP Object:TLV:IPV4-LSP-IDENTIFIERS:Extended Tunnel ID: %s' %
                                            (ipv4_lsp_identifiers_tlv[3]))
                                        logger.info(
                                            'PCEP PCRpt:LSP Object:TLV:IPV4-LSP-IDENTIFIERS:IPv4 Tunnel Endpoint Address: %s' %
                                            str(ipaddress.IPv4Address(ipv4_lsp_identifiers_tlv[4])))

                                    # PCRpt Object Unknown or Not Implemented TLV
                                    else:
                                        logger.info('PCEP PCRpt:Optional TLV:Unknown or Not Implemented TLV')
                                        offset = offset + tlv_len

                            # PCRpt BANDWIDTH Object
                            elif common_obj_hdr[0] == 5:
                                logger.info('PCRpt BANDWIDTH Object')

                                """
                                    0                   1                   2                   3
                                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                   |                        Bandwidth                              |
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                   '!f' = byte order:      network (= big-endian),
                                          byte0-3:         float (float)
                                """

                                bandwidth_obj = struct.unpack('!f',
                                                              data[offset: offset + obj_len - common_obj_hdr_len])

                                offset = offset + obj_len - common_obj_hdr_len

                                # PCRpt.bandwidth Dict (for JSON serialization) ------------------------------------- #
                                pcrpt_dict['bandwidth'] = bandwidth_obj[0]
                                # ----------------------------------------------------------------------------------- #

                                logger.info('PCEP PCRpt:BANDWIDTH Object:Bandwidth: %f' % bandwidth_obj[0])

                            # PCRpt RECORD ROUTE Object
                            elif common_obj_hdr[0] == 8:
                                logger.info('PCRpt RECORD ROUTE Object')

                                """
                                    0                   1                   2                   3
                                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                   |                                                               |
                                   //                        (Subobjects)                          //
                                   |                                                               |
                                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                   Sub-Object Header:

                                   '!BB' = byte order:    network (= big-endian),
                                           byte0:         unsigned char (integer),
                                           byte1:         unsigned char (integer)
                                """

                                # PCRpt.rro Dict (for JSON serialization) ------------------------------------------- #
                                pcrpt_dict['rro'] = []
                                # ----------------------------------------------------------------------------------- #

                                # Check for PCRpt RRO Object Sub-Objects
                                sub_objs_len = obj_len - common_obj_hdr_len
                                while sub_objs_len > 0:
                                    logger.info('PCEP PCRpt:RRO Object: Found Sub-Objects')

                                    sub_obj_hdr_len = 2

                                    sub_obj_hdr = struct.unpack('!BB',
                                                                data[offset: offset + sub_obj_hdr_len])

                                    offset = offset + sub_obj_hdr_len

                                    sub_obj_len = sub_obj_hdr[1]

                                    logger.info('PCEP PCRpt:RRO Object:Sub-Object:Type: %s' % sub_obj_hdr[0])
                                    logger.info('PCEP PCRpt:RRO Object:Sub-Object:Length: %i' % sub_obj_len)

                                    sub_objs_len = sub_objs_len - sub_obj_len

                                    # PCRpt RRO Object, Sub-Object IPv4 Prefix
                                    if sub_obj_hdr[0] == 1:
                                        logger.info('PCRpt:RRO Object:Sub-Object:IPv4 Prefix Sub-Object:Address:')

                                        """
                                            0                   1                   2                   3
                                            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                           |      Type     |     Length    | IPv4 address (4 bytes)        |
                                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                           | IPv4 address (continued)      | Prefix Length |      Flags    |
                                           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                                           '!IBB' = byte order:      network (= big-endian),
                                                    byte0-3:         unsigned int (integer),
                                                    byte4:           unsigned char (integer)
                                                    byte5:           unsigned char (integer)

                                        """

                                        ipv4_prefix = struct.unpack('!IBB',
                                                                    data[
                                                                    offset: offset + sub_obj_len - sub_obj_hdr_len])

                                        offset = offset + sub_obj_len - sub_obj_hdr_len

                                        # PCRpt.rro Dict (for JSON serialization) ----------------------------------- #
                                        pcrpt_dict['rro'].append({
                                            'address': str(ipaddress.IPv4Address(ipv4_prefix[0])),
                                            'prefix_length': ipv4_prefix[1],
                                            'local_protection_available': bool(ipv4_prefix[2] & 0b00000001),
                                            'local_protection_in_use': bool(ipv4_prefix[2] >> 1 & 0b00000001)
                                        })
                                        # --------------------------------------------------------------------------- #

                                        logger.info('PCRpt:RRO Object:Sub-Object:IPv4 Prefix Sub-Object:Address: %s' %
                                                    str(ipaddress.IPv4Address(ipv4_prefix[0])))

                                        logger.info(
                                            'PCRpt:RRO Object:Sub-Object:IPv4 Prefix Sub-Object:Prefix Length: %s' %
                                            ipv4_prefix[1])

                                        logger.info(
                                            'PCRpt:RRO Object:Sub-Object:IPv4 Prefix Sub-Object:Local Protection Available: %s' %
                                            (ipv4_prefix[2] & 0b00000001))

                                        logger.info(
                                            'PCRpt:RRO Object:Sub-Object:IPv4 Prefix Sub-Object:Local Protection In Use: %s' %
                                            (ipv4_prefix[2] >> 1 & 0b00000001))

                                    # PCRpt, RRO Object, Sub-Object Unknown or Not Implemented Sub-Object
                                    else:
                                        logger.info('PCRpt:RRO Object:Sub-Object Unknown or Not Implemented Sub-Object')
                                        offset = offset + sub_obj_len - sub_obj_hdr_len

                            # PCRpt Unknown or Not Implemented Object
                            else:
                                logger.info('PCRpt Unknown or Not Implemented Object')
                                offset = offset + obj_len - common_obj_hdr_len

                            logger.info('-----Object offset %i' % (offset - (message_length_sum - message_length)))
                            logger.info('-----Global offset %i' % offset)

                        logger.info('-----PCRpt Global offset %i' % offset)

                        # PCRpt callback (last or only PCRpt in sequence)
                        self.on_pcrpt(pcrpt=pcrpt_dict)

                    # PCEP Unknown or Not Implemented Message
                    else:
                        logger.info('PCEP Unknown or Not Implemented Message received')

                        # Process objects while computed local offset has not reached the end of the current message
                        while offset - (message_length_sum - message_length) != message_length:
                            common_obj_hdr = struct.unpack('!BBH',
                                                           data[offset:offset + common_obj_hdr_len])

                            offset = offset + common_obj_hdr_len
                            obj_len = common_obj_hdr[2]

                            logger.info('PCEP Object Class: %s' % common_obj_hdr[0])
                            logger.info('PCEP Object Type: %s' % (common_obj_hdr[1] >> 4))
                            logger.info('PCEP Reserved Flags: %s ' % (common_obj_hdr[1] >> 2 & 0b00000011))
                            logger.info('PCEP Processing-Rule (P): %s' % (common_obj_hdr[1] >> 1 & 0b00000001))
                            logger.info('PCEP Ignore (I): %s' % (common_obj_hdr[1] & 0b00000001))
                            logger.info('PCEP Object Length: %s' % obj_len)

                            offset = offset + obj_len - common_obj_hdr_len

                            logger.info(
                                '-----Unknown Object offset %i ' % (offset - (message_length_sum - message_length)))
                            logger.info('-----Global offset %i' % offset)

                        logger.info('-----Unknown Message Global offset %i' % offset)

        return True
