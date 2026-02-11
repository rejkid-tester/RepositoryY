cd C:\RepositoryY\certificate

# 1) Create Root CA
openssl genrsa -out RejkidRootCA.key 4096
openssl req -x509 -new -nodes -key RejkidRootCA.key -sha256 -days 1825 -out RejkidRootCA.crt `
  -subj "/CN=RejkidRootCA/O=MyOrganization/OU=MyDivision"

# 2) Create server key + CSR using config (adds SAN correctly)
openssl genrsa -out mycert.key 2048
openssl req -new -key mycert.key -out mycert.csr -config openssl-server.cnf

# 3) Sign server cert with CA
openssl x509 -req -in mycert.csr -CA RejkidRootCA.crt -CAkey RejkidRootCA.key -CAcreateserial `
  -out mycert.crt -days 825 -sha256 -extensions req_ext -extfile openssl-server.cnf

# 4) Export PFX for Kestrel (you will be prompted for a password)
openssl pkcs12 -export -out mycert.pfx -inkey mycert.key -in mycert.crt -certfile RejkidRootCA.crt