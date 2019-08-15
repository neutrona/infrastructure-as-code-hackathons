FROM alpine:latest
WORKDIR /root/
RUN apk --no-cache add ca-certificates
RUN apk add --update \
	git \
	go \
	musl-dev
RUN mkdir -p /root/go/src/k8s-api
RUN cd /root && go get -u github.com/gorilla/mux && go get -u k8s.io/client-go/... 

WORKDIR /root/go/src/k8s-api
COPY . .



RUN go build main.go

CMD [ "./main" ]
