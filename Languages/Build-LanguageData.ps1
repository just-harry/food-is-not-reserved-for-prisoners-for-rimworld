
[CmdletBinding()]
Param
(
	[Parameter()]
		[ValidateNotNull()]
			$OutputBasePath
)


$Translations = (& (Join-Path $PSScriptRoot Translations.ps1)).Translations


$XMLWriterSettings = [Xml.XmlWriterSettings]::new()
$XMLWriterSettings.Indent = $True
$XMLWriterSettings.IndentChars = "`t"


$AsLanguageDataXML = `
{
	Param ($Data)

	$XML = [Xml.XmlDocument]::new()
	$LanguageData = $XML.CreateElement('LanguageData')

	foreach ($Entry in $Data.GetEnumerator())
	{
		$Element = $XML.CreateElement($Entry.Key)
		$Element.InnerText = $Entry.Value
		$LanguageData.AppendChild($Element) > $Null
	}

	$XML.AppendChild($LanguageData) > $Null
	$XML
}


$WriteXMLTo = `
{
	Param ($XML, $Path)

	$Writer = [Xml.XmlWriter]::Create(
		$Global:ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path),
		$XMLWriterSettings
	)

	try
	{
		$XML.WriteTo($Writer)
	}
	finally
	{
		$Writer.Dispose()
	}
}


New-Item -ItemType Directory -Path $OutputBasePath -Force -ErrorAction Ignore > $Null


foreach ($Language in $Translations.GetEnumerator())
{
	$LanguagePath = Join-Path $OutputBasePath $Language.Key

	foreach ($File in $Language.Value.GetEnumerator())
	{
		$CompletePath = Join-Path $LanguagePath $File.Key
		$BasePath =  Split-Path -LiteralPath $CompletePath
		New-Item -ItemType Directory -Path $BasePath -Force -ErrorAction Ignore > $Null

		& $WriteXMLTo (& $AsLanguageDataXML $File.Value) "$CompletePath.xml"
	}
}

