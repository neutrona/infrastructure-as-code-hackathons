package main

import (
	"log"
	"time"

	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
)

func main() {
	config, err := rest.InClusterConfig()
	if err != nil {
		panic(err.Error())
	}

	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		panic(err.Error())
	}

	for {

		// pods, err := clientset.CoreV1().Pods("lab-nm2").List(metav1.ListOptions{})
		// if err != nil {
		// 	panic(err.Error())
		// }
		// fmt.Printf("There are %d pods in the cluster\n", len(pods.Items))

		// getss, err := clientset.AppsV1().StatefulSets("lab-nm2").Get("toolbox", metav1.GetOptions{})
		// if err != nil {
		// 	panic(err.Error())
		// }
		// fmt.Printf("Statefulset %v in namespace %v currently has %v replicas running\n", getss.Name, getss.Namespace, int(*getss.Spec.Replicas))

		ssscale, err := clientset.AppsV1().StatefulSets("lab-nm2").GetScale("toolbox", metav1.GetOptions{})

		switch {
		case int(ssscale.Spec.Replicas) == 1:
			ssscale.Spec.Replicas = int32(2)
			log.Println("Replicas has been set to 2")
		case int(ssscale.Spec.Replicas) == 2:
			ssscale.Spec.Replicas = int32(1)
			log.Println("Replicas has been set to 1")
		}

		_, err = clientset.AppsV1().StatefulSets("lab-nm2").UpdateScale("toolbox", ssscale)
		if err != nil {
			panic(err.Error())
		}

		// // Examples for error handling:
		// // - Use helper functions like e.g. errors.IsNotFound()
		// // - And/or cast to StatusError and use its properties like e.g. ErrStatus.Message
		// _, err = clientset.CoreV1().Pods("default").Get("example-xxxxx", metav1.GetOptions{})
		// if errors.IsNotFound(err) {
		// 	fmt.Printf("Pod not found\n")
		// } else if statusError, isStatus := err.(*errors.StatusError); isStatus {
		// 	fmt.Printf("Error getting pod %v\n", statusError.ErrStatus.Message)
		// } else if err != nil {
		// 	panic(err.Error())
		// } else {
		// 	fmt.Printf("Found pod\n")
		// }

		time.Sleep(10 * time.Second)
	}

}
