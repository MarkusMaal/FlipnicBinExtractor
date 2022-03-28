# Flipnic Game Data Extractor
This tool allows you to extract data from .BIN files using a properietary format. Despite being proprietary, the TOC format is not too difficult to understand.
## TOC explained
* TOC is divided into 64 byte chunks, each chunk begins with the filename (up to 60 characters) and ends with 4 pointer bytes.
* Each pointer is adressed by 2048 bytes.
* The first file in TOC is always "*Top Of CD Data", which identifies the beginning of the data stream.
* The last file is always "*End Of CD Data", which is the end offset of the .BIN file.
* Folders are identified by "\" at the end of the filename
* Folders cannot contain subfolders
* Each folder also has the same 64-byte chunk structure, but there is no first file identifying the start of data stream
* The last file in every folder is "*End Of Mem Data", identifying the end offset of the folder
* Each pointer is addressed by 1 byte, but to get the actual location you need to add the folder offset to the pointer value
* Same filenames are allowed
## Command line usage
* To extract data, use the following syntax: FlipnicBinExtractor /e [source] [destination]
* Repacking is not available in this version (but is still in development)
