package main

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"

	"github.com/gorilla/mux"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
)

// Statefulset
type Statefulset struct {
	Name     string `json:"Name"`
	Replicas int32  `json:"Replicas"`
}

func statefulSetsReplicas(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	namespace := vars["namespace"]
	statefulsetName := vars["statefulset"]

	config, err := rest.InClusterConfig()
	if err != nil {
		panic(err.Error())
	}

	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		panic(err.Error())
	}

	ssscale, err := clientset.AppsV1().StatefulSets(namespace).GetScale(statefulsetName, metav1.GetOptions{})

	log.Printf("Endpoint Hit: /statefulsets/replicas/%v/%v\n", namespace, statefulsetName)

	var myStatefulSet Statefulset
	myStatefulSet.Name = statefulsetName
	myStatefulSet.Replicas = ssscale.Spec.Replicas

	json.NewEncoder(w).Encode(myStatefulSet)
}

func updateStatefulSetsReplicas(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	namespace := vars["namespace"]
	statefulsetName := vars["statefulset"]
	replicas, err := strconv.ParseInt(vars["replicas"], 10, 32)

	config, err := rest.InClusterConfig()
	if err != nil {
		panic(err.Error())
	}

	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		panic(err.Error())
	}

	ssscale, err := clientset.AppsV1().StatefulSets(namespace).GetScale(statefulsetName, metav1.GetOptions{})

	log.Printf("Endpoint Hit: /statefulsets/replicas/%v/%v/update/%v\n", namespace, statefulsetName, replicas)

	// reqBody, err := ioutil.ReadAll(r.Body)
	// if err != nil {
	// 	http.Error(w, "Error reading request body", http.StatusInternalServerError)
	// }
	// var myStatefulSet Statefulset
	// json.Unmarshal(reqBody, &myStatefulSet)

	ssscale.Spec.Replicas = int32(replicas)
	_, err = clientset.AppsV1().StatefulSets(namespace).UpdateScale(statefulsetName, ssscale)
	if err != nil {
		panic(err.Error())
	}

	var myStatefulSet Statefulset
	myStatefulSet.Name = statefulsetName
	myStatefulSet.Replicas = ssscale.Spec.Replicas

	json.NewEncoder(w).Encode(myStatefulSet)
}

func handleRequests() {
	// creates a new instance of a mux router
	myRouter := mux.NewRouter().StrictSlash(true)

	myRouter.HandleFunc("/statefulsets/replicas/{namespace}/{statefulset}", statefulSetsReplicas)
	myRouter.HandleFunc("/statefulsets/replicas/{namespace}/{statefulset}/update/{replicas}", updateStatefulSetsReplicas).Methods("POST")

	log.Fatal(http.ListenAndServe(":10000", myRouter))
}

func main() {

	log.Println("Rest API v2.0 - Mux Routers")
	handleRequests()
}
