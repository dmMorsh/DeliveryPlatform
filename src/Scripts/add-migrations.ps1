function Get-MethodBody {
    param (
        [string]$content,
        [string]$methodName
    )

    if ($content -match "$methodName\(MigrationBuilder migrationBuilder\)[\s\r\n]*\{([\s\S]*?)\n\s*\}") {
        return $matches[1]
    }

    return ""
}

$ts = Get-Date -Format "yyyyMMddHHmmss"
$services = @(
    @{ Name = "Cart"; Cat = "CartService" },
    @{ Name = "Catalog"; Cat = "CatalogService" },
    @{ Name = "Couriers"; Cat = "CourierService" },
    @{ Name = "Inventory"; Cat = "InventoryService"; },
    @{ Name = "Orders";  Cat = "OrderService"; }
)

foreach ($svc in $services) {
    $name = "Auto_$ts"
    Write-Host "=== $($svc.Name) ==="
    dotnet ef migrations add $name `
        --project ../../src/$($svc.Cat)/$($svc.Cat).Infrastructure `
        --startup-project ../../src/$($svc.Cat)/$($svc.Cat).Api `
        --output-dir Persistence/Migrations `
    
    $migrationFile = Get-ChildItem ../../src/$($svc.Cat)/$($svc.Cat).Infrastructure/Persistence/Migrations `
    -Filter "*.cs" |
            Where-Object {
                $_.Name -notlike "*.Designer.cs" -and
                        $_.Name -notlike "*Snapshot.cs"
            } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
    
    $content = Get-Content $migrationFile.FullName -Raw
    
    Write-Host "=== Checking $($migrationFile.Name) ==="

    $upBody   = Get-MethodBody $content "Up"
    Write-Host "=== upBody $($upBody) ==="
    $downBody = Get-MethodBody $content "Down"
    Write-Host "=== downBody $($downBody) ==="
    
    $hasUpChanges   = $upBody   -match "migrationBuilder\."
    $hasDownChanges = $downBody -match "migrationBuilder\."

    if (-not $hasUpChanges -and -not $hasDownChanges)
    {
        Write-Host "Empty migration detected. Removing."
        dotnet ef migrations remove `
        --project ../../src/$($svc.Cat)/$($svc.Cat).Infrastructure `
        --startup-project ../../src/$($svc.Cat)/$($svc.Cat).Api `
    }
}