# Runs every time a package is uninstalled

param($installPath, $toolsPath, $package, $project)

# $installPath is the path to the folder where the package is installed.
# $toolsPath is the path to the tools directory in the folder where the package is installed.
# $package is a reference to the package object.
# $project is a reference to the project the package was installed to.

function getProjectDirectory($project)
{
	$projectFullName = $project.FullName
	$fileInfo = new-object -typename System.IO.FileInfo -ArgumentList $projectFullName
	return $fileInfo.DirectoryName
}

function cleanServieParameters($parmsElm, $prefix){
    if ($parmsElm){     
        for ($i=$parmsElm.ChildNodes.Count-1; $i-ge 0;$i--){
            if ($parmsElm.ChildNodes[$i].Name.StartsWith($prefix)){
                $parmsElm.RemoveChild($parmsElm.ChildNodes[$i]);
            }
        }
    }
}

function cleanAppManifest($appXml, $srvXml) {
    $importElements = $appXml.ApplicationManifest.ServiceManifestImport | where {$_.ServiceManifestRef.ServiceManifestName -eq $srvXml.ServiceManifest.Name}
    Foreach($importElement in $importElements){
        $importElement.ParentNode.RemoveChild($importElement)
    }

    Foreach($srvType in $srvXml.ServiceManifest.ServiceTypes.ChildNodes) {
        if ($srvType.Name -eq "StatelessServiceType") {
            $defaultServices = $appXml.ApplicationManifest.DefaultServices.Service | where {$_.StatelessService.ServiceTypeName -eq $srvType.ServiceTypeName}
            Foreach($defaultService in $defaultServices){
                $defaultService.ParentNode.RemoveChild($defaultService)
            }
        } elseif ($srvType.Name -eq "StatefulServiceType") {
            $defaultServices = $appXml.ApplicationManifest.DefaultServices.Service | where {$_.StatefulService.ServiceTypeName -eq $srvType.ServiceTypeName}
            Foreach($defaultService in $defaultServices){
                $defaultService.ParentNode.RemoveChild($defaultService)
            }
        }
    }
	
	$dftServiceCount = $appXml.ApplicationManifest.DefaultServices.ChildNodes.Count
	if ($dftServiceCount -eq 0){
        $elm = $appXml.ApplicationManifest.GetElementsByTagName("DefaultServices")
    	$appXml.ApplicationManifest.RemoveChild($elm[0])
	}

    $srvName = $srvXml.ServiceManifest.Name.Substring(0, $srvXml.ServiceManifest.Name.Length-3)
    cleanServieParameters $appXml.ApplicationManifest.Parameters ($srvName+"_")
    $parmCount = $appXml.ApplicationManifest.Parameters.ChildNodes.Count
    if ($parmCount -eq 0){
        $pElm = $appXml.ApplicationManifest.GetElementsByTagName("Parameters")
        $appXml.ApplicationManifest.RemoveChild($pElm[0])
    }
}

$srcFolder = Get-Item $installPath\*Pkg | Where-Object {$_.Mode -match 'd'}
$destFolder = getProjectDirectory($project)
$destFolder = "$destFolder\ApplicationPackageRoot"

$appMainfest = "$destFolder\ApplicationManifest.xml"
$srvManifest = "$destFolder\" + $srcFolder.Name + "\ServiceManifest.xml"

$appXml = [xml](Get-Content $appMainfest)
$srvXml = [xml](Get-Content $srvManifest)

cleanAppManifest $appXml $srvXml

$appXml.Save($appMainfest)


$project.ProjectItems.Item("ApplicationPackageRoot").ProjectItems.Item($srcFolder.Name).Remove()
Remove-Item ("$destFolder\" + $srcFolder.Name) -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item ("$destFolder\" + $srcFolder.Name) -Recurse -Force -ErrorAction SilentlyContinue
