$ip  = "192.168.0.15"
$dns = "rejkid.local"

$san = @(
  $dns,
  "backend.rejkid.local",
  $ip
)

$cert = New-SelfSignedCertificate `
  -Subject "CN=$dns" `
  -DnsName $san `
  -CertStoreLocation "Cert:\LocalMachine\My" `
  -KeyAlgorithm RSA -KeyLength 2048 `
  -HashAlgorithm sha256 `
  -KeyUsage DigitalSignature `
  -NotAfter (Get-Date).AddYears(2) `
  -KeyExportPolicy Exportable `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")  # EKU=ServerAuth

# Export PFX (contains private key) + CER (public only)
$pwd = Read-Host "PFX password" -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath "C:\RepositoryY\certificate\mycert.pfx" -Password $pwd
Export-Certificate    -Cert $cert -FilePath "C:\RepositoryY\certificate\mycert.cer"