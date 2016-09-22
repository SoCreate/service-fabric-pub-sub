# Runs every time a package is installed in a project

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

function addFiles($project, $srcFolder, $destFolder)
{
    $project.ProjectItems.Item("ApplicationPackageRoot").ProjectItems.AddFromDirectory($srcFolder)
}
function appendAttribute($xml, $element, [string]$name, [string]$value) {
    $attribute = $xml.CreateAttribute($name)
    $attribute.Value = $value
    $element.Attributes.Append($attribute)
}
function addParameter($appXml, $parms, $parmName, $parmValue){
	$parm = $appXml.CreateElement("Parameter", $nsm.DefaultNamespace)
	appendAttribute $appXml $parm "Name" $parmName 
    appendAttribute $appXml $parm "DefaultValue" $parmValue
	$parms.AppendChild($parm)
}
function updateAppManifest($appXml, $srvXml, $appOverridesXml) {
    $nsm = New-Object System.Xml.XmlNamespaceManager($appXml.NameTable)
    $nsm.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema")
    $nsm.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
    $nsm.AddNamespace("","http://schemas.microsoft.com/2011/01/fabric")

    $importElement = $appXml.CreateElement("ServiceManifestImport", $nsm.DefaultNamespace)
    $manifestRef = $appXml.CreateElement("ServiceManifestRef", $nsm.DefaultNamespace)
    appendAttribute $appXml $manifestRef "ServiceManifestName" $srvXml.ServiceManifest.Name
    appendAttribute $appXml $manifestRef "ServiceManifestVersion" $srvXml.ServiceManifest.Version
    $importElement.AppendChild($manifestRef)

    $appXml.ApplicationManifest.PrependChild($importElement)

    $dftSrvElement = $appXml.ApplicationManifest.DefaultServices
    if (!$dftSrvElement){
        $dftSrvElement = $appXml.CreateElement("DefaultServices", $nsm.DefaultNamespace)
        $dftSrvElement = $appXml.ApplicationManifest.AppendChild($dftSrvElement)
    }

	$parmElement = $appXml.ApplicationManifest.Parameters
	if (!$parmElement){
		$parmElement = $appXml.CreateElement("Parameters", $nsm.DefaultNamespace)
	}

    $srvElement = $appXml.CreateElement("Service", $nsm.DefaultNamespace)
	$srvName = $srvXml.ServiceManifest.Name.Substring(0, $srvXml.ServiceManifest.Name.Length-3)
    appendAttribute $appXml $srvElement "Name" $srvName

    Foreach($srvType in $srvXml.ServiceManifest.ServiceTypes.ChildNodes) {
        if ($srvType.Name -eq "StatelessServiceType") {
            $stElement = $appXml.CreateElement("StatelessService", $nsm.DefaultNamespace)
            appendAttribute $appXml $stElement "ServiceTypeName" $srvType.ServiceTypeName
            appendAttribute $appXml $stElement "InstanceCount" ("["+$srvName+"_InstanceCount]")
            $partElement = $appXml.CreateElement("SingletonPartition", $nsm.DefaultNamespace)
			addParameter $appXml $parmElement ($srvName+"_InstanceCount") "-1"
            $stElement.AppendChild($partElement)
            $srvElement.AppendChild($stElement)
        }
		elseif ($srvType.Name -eq "StatefulServiceType") {
			# Don't gerenerate DefaultService entries for Actors. I'm not sure if this is the best way to tell actors from stateful services
			$elm = ($srvType.Extensions.Extension | Where-Object {$_.Name -eq '__GeneratedServiceType__'})
			if (!$elm.Name) {
				$stElement = $appXml.CreateElement("StatefulService", $nsm.DefaultNamespace)
				appendAttribute $appXml $stElement "ServiceTypeName" $srvType.ServiceTypeName
				appendAttribute $appXml $stElement "TargetReplicaSetSize" ("["+$srvName+"_TargetReplicaSetSize]")
				addParameter $appXml $parmElement ($srvName+"_TargetReplicaSetSize") "3"
				appendAttribute $appXml $stElement "MinReplicaSetSize" ("["+$srvName+"_MinReplicaSetSize]")
				addParameter $appXml $parmElement ($srvName+"_MinReplicaSetSize") "3"
				$partElement = $appXml.CreateElement("UniformInt64Partition", $nsm.DefaultNamespace)
				appendAttribute $appXml $partElement "PartitionCount" ("["+$srvName+"_PartitionCount]")
				addParameter $appXml $parmElement ($srvName+"_PartitionCount") "1"
				appendAttribute $appXml $partElement "LowKey" "-9223372036854775808"
				appendAttribute $appXml $partElement "HighKey" "9223372036854775807"
				$stElement.AppendChild($partElement)
				$srvElement.AppendChild($stElement)
			}
		}
    }

	if ($appOverridesXml) {
        $overrideElm = ($appOverridesXml.ApplicationManifest.ServiceManifestImport | Where-Object {$_.ServiceManifestRef.ServiceManifestName -eq $srvXml.ServiceManifest.Name})
        if ($overrideElm.Policies){
                    $importElement.AppendChild($appXml.ImportNode($overrideElm.Policies, $true))
        }
        $principleElm = $appOverridesXml.ApplicationManifest.Principals
        if ($principleElm){
            $appXml.ApplicationManifest.AppendChild($appXml.ImportNode($principleElm, $true))
        }
    }

	if ($appXml.ApplicationManifest.Parameters) {
        $appXml.ApplicationManifest.InsertAfter($importElement, $appXml.ApplicationManifest.Parameters)
    } else {
        $appXml.ApplicationManifest.PrependChild($importElement)
    }

	if ($srvElement.ChildNodes.Count -gt 0) {
		$dftSrvElement.AppendChild($srvElement)
	}
}

function updateNuspec($nugetXml, $id, $version){
    $count= $nugetXml.package.metadata.GetElementsByTagName("dependencies").Count
    if ($count -eq 0){
        $dependencies = $nugetXml.CreateElement("dependencies")
        $nugetXml.package.metadata.AppendChild($dependencies)        
    } else {
        $dependencies = $nugetXml.package.metadata.GetElementsByTagName("dependencies")[0]
    }
    $dependency = $nugetXml.CreateElement("dependency")
    appendAttribute $nugetXml $dependency "id" $id
    appendAttribute $nugetXml $dependency "version" $version
    $dependencies.AppendChild($dependency)
}

$srcFolder = Get-Item $installPath\*Pkg | Where-Object {$_.Mode -match 'd'}
$destFolder = getProjectDirectory($project)

if ([System.IO.Directory]::Exists("$destFolder\ApplicationPackageRoot")) {
	$destFolder = "$destFolder\ApplicationPackageRoot"
	addFiles $project $srcFolder ($destFolder+"\"+$srcFolder.Name)

	$appMainfest = "$destFolder\ApplicationManifest.xml"
	$srvManifest = "$srcFolder\ServiceManifest.xml"
	$appManifestOverrides = "$srcFolder\ApplicationManifest.overrides.xml"

	$appXml = [xml](Get-Content $appMainfest)
	$srvXml = [xml](Get-Content $srvManifest)
	if ([System.IO.File]::Exists($appManifestOverrides)) {
		$appOverridesXml = [xml](Get-Content $appManifestOverrides)
	} else {
		$appOverridesXml = $null
	}

	updateAppManifest $appXml $srvXml $appOverridesXml

	$appXml.Save($appMainfest)
} elseif ([System.IO.File]::Exists("$destFolder\Package.nuspec")) {
    $nugetXml = [xml](Get-Content "$destFolder\Package.nuspec")
	updateNuspec $nugetXml $package.Id $package.Version
    $nugetXml.Save("$destFolder\Package.nuspec")
} else {
	Write-Error "SFNuGet can't be installed for this project type."
	exit 1
}
