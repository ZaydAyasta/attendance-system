[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [Uri]$BaseUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($BaseUrl.Scheme -ne 'https') {
    throw 'BaseUrl must use HTTPS.'
}

if ($BaseUrl.AbsolutePath -ne '/' -or
    -not [string]::IsNullOrEmpty($BaseUrl.Query) -or
    -not [string]::IsNullOrEmpty($BaseUrl.Fragment)) {
    throw 'BaseUrl must not include a path, query, or fragment. Provide only the public application origin.'
}

$origin = $BaseUrl.GetLeftPart([System.UriPartial]::Authority)
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(20)

try {
    function Assert-Status {
        param(
            [Parameter(Mandatory)] [System.Net.Http.HttpMethod]$Method,
            [Parameter(Mandatory)] [string]$Path,
            [Parameter(Mandatory)] [int]$ExpectedStatus
        )

        $request = [System.Net.Http.HttpRequestMessage]::new($Method, "$origin$Path")
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        if ([int]$response.StatusCode -ne $ExpectedStatus) {
            $response.Dispose()
            throw "$Method $Path returned $([int]$response.StatusCode), expected $ExpectedStatus."
        }

        Write-Host "[OK] $Method $Path -> $ExpectedStatus"
        return $response
    }

    $root = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/' -ExpectedStatus 200
    try {
        if ($root.Content.Headers.ContentType.MediaType -ne 'text/html') {
            throw "GET / returned '$($root.Content.Headers.ContentType.MediaType)' instead of text/html."
        }
    }
    finally {
        $root.Dispose()
    }

    $login = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/login' -ExpectedStatus 200
    $login.Dispose()

    $live = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/health/live' -ExpectedStatus 200
    $live.Dispose()

    $ready = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/health/ready' -ExpectedStatus 200
    $ready.Dispose()

    $unknownApi = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/api/not-a-real-endpoint' -ExpectedStatus 404
    $unknownApi.Dispose()

    $unknownAsset = Assert-Status -Method ([System.Net.Http.HttpMethod]::Get) -Path '/assets/not-a-real-asset.js' -ExpectedStatus 404
    $unknownAsset.Dispose()

    $unknownMutation = Assert-Status -Method ([System.Net.Http.HttpMethod]::Post) -Path '/not-a-real-endpoint' -ExpectedStatus 404
    $unknownMutation.Dispose()
}
finally {
    $client.Dispose()
}

Write-Host 'Production smoke test passed.'
