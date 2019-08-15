FROM alpine:latest
WORKDIR /root/
RUN apk --no-cache add ca-certificates
RUN apk add --update \
	git \
	go \
	musl-dev
RUN mkdir -p /root/go/src/pcep-listener
WORKDIR /root/go/src/pcep-listener
COPY . .

RUN cd /root && go get github.com/streadway/amqp && go get github.com/prometheus/client_golang/prometheus/promhttp
RUN go build main.go

CMD [ "./main" ]
