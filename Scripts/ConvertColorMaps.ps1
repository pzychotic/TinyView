# converts color maps from https://github.com/sjmgarnier/viridisLite/tree/master/data-raw/option*.csv to C# byte arrays

$mapNames = @{
    "./optionA.csv" = "Magma"
    "./optionB.csv" = "Inferno"
    "./optionC.csv" = "Plasma"
    "./optionD.csv" = "Viridis"
    "./optionE.csv" = "Cividis"
    "./optionF.csv" = "Rocket"
    "./optionG.csv" = "Mako"
    "./optionH.csv" = "Turbo"
}

$filename  = "./ColorMaps.cs"
$namespace = "TinyView"
$classname = "ColorMaps"

$fileHeader = `
"// Automatically generated from https://github.com/sjmgarnier/viridisLite/tree/master/data-raw/option*.csv

namespace $namespace
{
    static class $classname
    {"
$fileFooter = `"    }
}"


Set-Content -Path $filename -Value $fileHeader

$mapNames.GetEnumerator() | ForEach-Object {

    "        public static readonly byte[,] $($_.Value) ="
    "        {"

    Import-Csv -Path $_.Key |
        ForEach-Object {
            $r = [math]::Round( ([double] $_.R) * 255.0 )
            $g = [math]::Round( ([double] $_.G) * 255.0 )
            $b = [math]::Round( ([double] $_.B) * 255.0 )
            "            {$r, $g, $b},"
        }

    "        };"
    ""
} | Add-Content -Path $filename

Add-Content -Path $filename -Value $fileFooter
