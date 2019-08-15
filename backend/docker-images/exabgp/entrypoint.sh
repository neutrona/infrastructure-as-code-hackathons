#!/usr/bin/env bash
echo $BGP_PEER
echo $RID
echo $LOCAL_IP
echo $LOCAL_AS
echo $PEER_AS

cat >> /opt/exabgp/exabgp.conf << EOF
neighbor $BGP_PEER {
    router-id $RID;
    local-address $LOCAL_IP;
    local-as $LOCAL_AS;
    peer-as $PEER_AS;
    api {
        processes [ parsed-route-backend ];
        receive {
            parsed;
            update;
        }
    }
    api {
        processes [ mastership_handler ];
        receive {
            parsed;
            notification;
            keepalive;
        }
   }
}
EOF

cat /opt/exabgp/exabgp.conf

/usr/bin/exabgp /opt/exabgp/exabgp.conf