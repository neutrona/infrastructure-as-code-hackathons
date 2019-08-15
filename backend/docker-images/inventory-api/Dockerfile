FROM alpine:latest
WORKDIR /root/
RUN apk --no-cache add ca-certificates
RUN apk add --update \
	python3
COPY . .
RUN pip3 install --no-cache-dir -r requirements.txt
CMD [ "python3", "inventory_api.py" ]
