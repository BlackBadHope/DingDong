# Downloads only the pinned author source; compiles locally using official tML.
function Get-DDOreSourcePin {
    [pscustomobject]@{commit='952d0c71ff76d6c140aca83f5d3a1d7a2ab0fe98';url='https://codeload.github.com/CrazyContraption/OreExcavator/zip/952d0c71ff76d6c140aca83f5d3a1d7a2ab0fe98';sourceBytes=260066L;sourceSha256='1446a93557b3825b943204eab60f4734e93d35a86cb666ccdc41687fb5fb1d3c';name='OreExcavator';version='0.8.9';bytes=231379L;sha256='423e1d075a63dc7db6a03f0cfe0b08e798f38f87ff83823044bc4732a9c6701c'}
}
function Receive-DDOfficialOre([string]$Directory,[string[]]$ForbiddenRoots,[string]$OfficialTml,[string]$Compiler) {
    $pin=Get-DDOreSourcePin;$cache=[IO.Path]::GetFullPath($Directory).TrimEnd('\');Assert-DDRegularPath $cache
    foreach($root in $ForbiddenRoots){if($root){$full=[IO.Path]::GetFullPath($root).TrimEnd('\');if($cache -ieq $full -or $cache.StartsWith($full+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Official source build cache overlaps protected Steam/profile data.'}}}
    [void][IO.Directory]::CreateDirectory($cache)
    $output=Join-Path $cache 'OreExcavator.tmod'
    if(Test-Path -LiteralPath $output){try{Assert-DDExactPackage $output $pin;return $output}catch{Move-Item -LiteralPath $output -Destination ($output+'.corrupt-'+[Guid]::NewGuid().ToString('N'))}}
    $source=Join-Path $cache ('OreExcavator-source-'+$pin.commit+'.zip')
    if((Test-Path -LiteralPath $source) -and ((Get-Item -LiteralPath $source).Length -ne $pin.sourceBytes -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -cne $pin.sourceSha256)){Move-Item -LiteralPath $source -Destination ($source+'.corrupt-'+[Guid]::NewGuid().ToString('N'))}
    if(!(Test-Path -LiteralPath $source)){
        [Net.ServicePointManager]::SecurityProtocol=[Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $request=[Net.HttpWebRequest]::Create($pin.url);$request.AllowAutoRedirect=$false;$request.Timeout=30000;$request.ReadWriteTimeout=30000;$request.UserAgent='DingDong-Official-Source-Build/1'
        $response=$request.GetResponse();$partial=$source+'.part'
        try {
            if([int]$response.StatusCode -ne 200 -or ($response.ContentLength -gt 0 -and $response.ContentLength -ne $pin.sourceBytes)){throw 'Author source HTTP size/status mismatch.'}
            $input=$response.GetResponseStream();$out=[IO.File]::Open($partial,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$buffer=New-Object byte[] 32768;$total=0L;while(($read=$input.Read($buffer,0,$buffer.Length)) -gt 0){$total+=$read;if($total -gt $pin.sourceBytes){throw 'Author source exceeds its pinned size.'};$out.Write($buffer,0,$read)}}finally{$out.Dispose();$input.Dispose()}
            if((Get-Item -LiteralPath $partial).Length -ne $pin.sourceBytes -or (Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant() -cne $pin.sourceSha256){throw 'Author source checksum mismatch.'}
            Move-Item -LiteralPath $partial -Destination $source
        }finally{$response.Dispose()}
    }
    Write-Host 'Building OreExcavator 0.8.9 locally from the pinned author commit (no SDK required)...'
    $temporary=$output+'.'+[Guid]::NewGuid().ToString('N')+'.pending'
    $previousErrorAction=$ErrorActionPreference
    try{$ErrorActionPreference='Continue';$text=@(& (Join-Path $OfficialTml 'dotnet\dotnet.exe') $Compiler $OfficialTml $source $pin.sourceSha256 $temporary 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$previousErrorAction}
    if($code -ne 0){throw 'Official Ore source build failed; no installed mod was replaced.'}
    Assert-DDExactPackage $temporary $pin
    Move-Item -LiteralPath $temporary -Destination $output
    $output
}
