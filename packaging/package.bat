set packagesDir="packages"
if not exist %packagesDir% (mkdir %packagesDir%)
nuget pack "..\src\Trollbridge.Core\Trollbridge.Core\Trollbridge.Core.csproj" -Build -Prop Configuration=Release -OutputDirectory %packagesDir%
explorer %packagesDir%