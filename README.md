# Flipnic Game Data Extractor
This tool allows you to extract data from .BIN files using a properietary format. Despite being proprietary, the TOC format is not too difficult to understand.<br/><br/>
![image](https://user-images.githubusercontent.com/45605071/160381952-011b5c21-e050-474c-9942-36308ad89d3e.png)
## TOC explained
* TOC is divided into 64 byte chunks, each chunk begins with the filename (up to 60 characters) and ends with 4 pointer bytes.
* Each pointer is addressed by 2048 bytes (0x800).
* The first file in TOC is always "*Top Of CD Data", which identifies the beginning of the data stream.
* The last file is always "*End Of CD Data", which is the end offset of the .BIN file.
* Folders are identified by "\\" at the end of the filename
* Folders cannot contain subfolders
* Each folder also has the same 64-byte chunk structure, but there is no first file identifying the start of data stream
* The last file in every folder is "*End Of Mem Data", identifying the end offset of the folder
* Each pointer is addressed by 1 byte (0x1), but to get the actual location you need to add the folder offset to the pointer value
* Same filenames are allowed
## Command line usage
* To extract data, use the following syntax: `FlipnicBinExtractor /e [source] [destination]`
* To list directory, use the following syntax: `FlipnicBinExtractor /l [source]`
* To data to a subfolder, use the following syntax: `FlipnicBinExtractor /f [source] [destination]`
* Full BIN repacking is not available in this version (but is still in development)
## Examples
* `FlipnicBinExtractor /e RES.BIN Extracted`
* `FlipnicBinExtractor /e FONT.BIN FONT`
* `FlipnicBinExtractor /l RES.BIN`
* `FlipnicBinExtractor /f BOSS1 BOSS1\A`
* `FlipnicBinExtractor /c FONT FONT.BIN` (not available in current version)
## VGMToolBox settings
These settings can be used in VGMToolBox (VGMToolbox > Misc. Tools > Extraction Tools > Generic > Virtual File System Extractor) for extracting BIN files (note that this will only work if the BIN file has no subfolders, you will need to use my tool for more advanced extraction purposes):
* Header Size or File Count > Header Ends at Offset `<offset of the last "*End Of CD Data" file>`
* File Record Information > File Records begin at offset `0` and each record has size `0x40`
* Individual File Offset > File Offset is at Offset `60` and has size `4` and byte order `Little Endian`
	* and do calculation (use $V to represent the value at the offset) `$V*2048`
* Individual File Length > Use File Offsets to determine File Lengths
* Individual File Name Location/Offset > File Name is Included in the Individual File Record at Offset `0`
* Individual File Name Size > Has Static Size `59`
