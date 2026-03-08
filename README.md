# MyrientDL
Downloader CLI for Myrient website. Wrote this over a weekend during the final month of the Myrient servers being up.

<img width="942" height="208" alt="image" src="https://github.com/user-attachments/assets/4f883373-3ea0-4e6c-a747-be189c633395" />
<br/><br/>

Executables are available for Linux (x64 and arm64), Windows (x64), and MacOS (x64 and arm64).

# Usage
Run the executable. There will be two subsequent prompts for inputting both the Myrient files directory path you wish to download, as well as the output path were the files should be downloaded.

## Example
<img width="942" height="258" alt="image" src="https://github.com/user-attachments/assets/33415082-7f62-4efd-bb84-1a2ac8d2b2bc" />
<br/><br/>

The directory and subdirectories (if any) will be scanned and a download list will be generated. The file name is the SHA1 hash of the given Myrient files URL.

The download list records all of the directories, subdirectories, and files with their respective file sizes. Files that have already been downloaded are marked accordingly which allows for the download process to be resumed if it is interrupted in any way.

If a download list has already been generated, then the Myrient site will not be scanned and instead, the download list will be used.

Afterwards, a summary of the content to be downloaded will be displayed along with a final confirmation prompt to continue:

<img width="942" height="394" alt="image" src="https://github.com/user-attachments/assets/086d4958-ae56-4003-ae94-8d47883b91e9" />
<br/><br/>

Upon successful confirmation, the downloads will begin with progess indicators for percentage downloaded, estimated time remaining, and download speed for each file:

<img width="942" height="370" alt="image" src="https://github.com/user-attachments/assets/fdfbb949-1dae-4255-b4e0-3a47349db168" />
