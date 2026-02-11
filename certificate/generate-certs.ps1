$ErrorActionPreference = 'Stop'

$certDir = "C:\RepositoryY\certificate"
New-Item -ItemType Directory -Force -Path $certDir | Out-Null

$cnfPath = Join-Path $certDir 'openssl-server.cnf'
if (-not (Test-Path $cnfPath)) {
    throw "Missing config file: $cnfPath"
}

Push-Location $certDir
try {
    Write-Host "Generating Root CA + server certificate in $certDir"

    # Root CA
    openssl genrsa -out RejkidRootCA.key 4096
    openssl req -x509 -new -nodes -key RejkidRootCA.key -sha256 -days 1825 -out RejkidRootCA.crt `
        -subj "/CN=RejkidRootCA/O=MyOrganization/OU=MyDivision"

    # Server key + CSR (SAN/eku/ku from openssl-server.cnf)
    openssl genrsa -out mycert.key 2048
    openssl req -new -key mycert.key -out mycert.csr -config $cnfPath

    # Sign server cert with CA
    openssl x509 -req -in mycert.csr -CA RejkidRootCA.crt -CAkey RejkidRootCA.key -CAcreateserial `
        -out mycert.crt -days 825 -sha256 -extensions req_ext -extfile $cnfPath

    # Export PFX for Kestrel (prompts for password)
    openssl pkcs12 -export -out mycert.pfx -inkey mycert.key -in mycert.crt -certfile RejkidRootCA.crt

    Write-Host "\nOutputs:"
    Write-Host "  CA cert (trust this):   $certDir\RejkidRootCA.crt"
    Write-Host "  Server cert (public):   $certDir\mycert.crt"
    Write-Host "  Server cert for Kestrel:$certDir\mycert.pfx"

    Write-Host "\nVerify SAN contains DNS+IP:"
    openssl x509 -in mycert.crt -noout -text | Select-String -Pattern 'Subject Alternative Name' -Context 0, 2

    Write-Host "\nTo trust the CA (run PowerShell as Admin):"
    Write-Host "  Import-Certificate -FilePath $certDir\RejkidRootCA.crt -CertStoreLocation Cert:\LocalMachine\Root"

    Write-Host "\nKestrel user-secrets keys:"
    Write-Host "  Kestrel:Endpoints:Https:Certificate:Path     = $certDir\mycert.pfx"
    Write-Host "  Kestrel:Endpoints:Https:Certificate:Password = <the PFX export password>"
}
finally {
    Pop-Location
}
