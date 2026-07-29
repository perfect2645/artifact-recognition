# Self-signed-certificate

## security

- don't publish your private key to git (add gitignore for private key)

## create certs

- create a self-signed-certificate for micro-services

1. Open git bash with admin user
2. cd cert target path (cd /d/secrets/certs/local-certs/root)
3. generate independent <orange>root CA</orange>

```bash
# generate root CA private key
openssl genrsa -out fawei-rootCA.key 2048

# create root CA Configuration

cat > root-ca.cnf << EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
x509_extensions = v3_ca

[dn]
C = CN
ST = Liaoning
L = Dalian
O = Fawei
CN = Fawei Local Dev Root CA

[v3_ca]
basicConstraints = critical, CA:TRUE
keyUsage = critical, keyCertSign, cRLSign
subjectKeyIdentifier = hash
authorityKeyIdentifier = keyid:always, issuer
EOF

# generate root CA certificate（Valid for 10 years）
openssl req -x509 -new -nodes \
  -key fawei-rootCA.key \
  -sha256 -days 3650 \
  -out fawei-rootCA.crt \
  -config root-ca.cnf

```

4. verify Root CA cert

```bash

# true means valid
openssl x509 -in fawei-rootCA.crt -text -noout | grep "CA:"

```

5. install Root CA cert

6. create <orange>artifact</orange> openssl config

```bash

cat > artifact.openssl.cnf << EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = v3_req

[dn]
CN = localhost-artifact

[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = localhost-artifact
IP.1 = 127.0.0.1
IP.2 = 192.168.31.122
EOF

```

7. Issue service certificates for Artifact using the root CA

```bash

# 1. generate private key
openssl genrsa -out localhost-artifact.key 2048

# 2. Generate a Certificate Signing Request (CSR)
openssl req -new -key localhost-artifact.key -out localhost-artifact.csr -config artifact.openssl.cnf

# 3. generate CA-signed cert (valid for 10 years)
openssl x509 -req -days 3650 \
  -in localhost-artifact.csr \
  -CA ../root/fawei-rootCA.crt \
  -CAkey ../root/fawei-rootCA.key \
  -CAcreateserial \
  -out localhost-artifact.crt \
  -sha256 \
  -extfile artifact.openssl.cnf \
  -extensions v3_req

# 4. generate .pfx cert
openssl pkcs12 -export -out localhost-artifact.pfx -inkey localhost-artifact.key -in localhost-artifact.crt -password pass:asdf1234

# 5. generate windows type cert (.cer)
cp localhost-artifact.crt localhost-artifact.cer

# 6. generate pem type cert
# 6-1. Export certificate only (without private key)
openssl pkcs12 -in localhost-artifact.pfx -clcerts -nokeys -out localhost-artifact-cert.pem -passin pass:asdf1234

# 6-2. Export Private Key Only (No Password Protection)
openssl pkcs12 -in localhost-artifact.pfx -nocerts -nodes -out localhost-artifact-key.pem -passin pass:asdf1234

# 7. verify certs
# verify issuer
openssl x509 -in localhost-artifact.crt -text -noout | grep "Issuer"

# Check whether the SAN is effective
openssl x509 -in localhost-artifact.crt -text -noout | grep -A 5 "Subject Alternative Name"

```

## webapi config

1. add pfx cert to webapi prject, set copy to output folder when build
2. update webapi appsettings.json

```
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:7092",
        "Certificate": {
          "Path": "Configurations/certs/localhost-artifact.pfx",
          "Password": "asdf1234",
          "AllowInvalid": false
        }
      }
    }
  }
```

## react config

1. place cert to react project

```
your-react-project/
├── ssl/
│   ├── localhost-doraemon-cert.pem  # 证书公钥
│   └── localhost-doraemon-key.pem   # 证书私钥
├── src/
├── package.json
└── vite.config.js/ts
```

2. update vite.conifg.js

```
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  server: {
    https: {
      cert: path.resolve(__dirname, './ssl/localhost-doraemon-cert.pem'),
      key: path.resolve(__dirname, './ssl/localhost-doraemon-key.pem'),
    },
    port: 3000,
    open: true,
  },
});
```
