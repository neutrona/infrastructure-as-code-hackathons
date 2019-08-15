#!/usr/bin/env bash

cat > /etc/nginx/sites-enabled/db-lb.conf << EOF
upstream neo4jdb { 
    server neo4j-$REGION_1:7474;
}

server {
    listen 7474;
    location / {
        proxy_pass http://neo4jdb;
    }
}

EOF

cat >> /etc/nginx/nginx.conf << EOF

stream {
  server {
    listen 7687;
    proxy_pass neo4j-$REGION_1:7687;
  }
}

EOF

python3 lb-api.py