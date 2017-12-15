Param(
    [Parameter(Mandatory=$True,Position=1)]
    [string] $pfxPwd
)

[string] $pfxFilePath = ".\ServiceFabric.PubSubActors.pfx"
[string] $snkFilePath = [IO.Path]::GetFileNameWithoutExtension($pfxFilePath) + ".snk";
[byte[]] $certificateContent = Get-Content $pfxFilePath -Encoding Byte;
$exportable = [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certificateContent, $pfxPwd, $exportable)
$certificateContent = ([Security.Cryptography.RSACryptoServiceProvider]$certificate.PrivateKey).ExportCspBlob($true)
[IO.File]::WriteAllBytes([IO.Path]::Combine([IO.Path]::GetDirectoryName($pfxFilePath), $snkFilePath), $certificateContent)