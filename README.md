# WTF – Where’s The Filespace

WTF is a lightweight Windows disk space analyzer for quickly finding where storage is used.
Multilingual support comming, when requested - (Now: English/German)


# Main Window

<br>
<img width="1437" height="948" alt="grafik" src="https://github.com/user-attachments/assets/895e9169-f486-4d97-9348-0141c89ea485" />
<br>
<br>

# Storage History

<br>
<img width="1346" height="711" alt="grafik" src="https://github.com/user-attachments/assets/916c6caa-97e9-4b2f-8833-c6f3b088d9ce" />
<br>
<br>


# Pie-Chart / Tableview

<br>
<img width="1158" height="819" alt="grafik" src="https://github.com/user-attachments/assets/28fd3c50-7257-4b54-b165-c741895820f0" />
<br>
<img width="1158" height="819" alt="grafik" src="https://github.com/user-attachments/assets/eb672365-138a-4878-8db2-55b751f54540" />
<br>
<br>

# Scan History / Comparison

<br>
<img width="1155" height="552" alt="grafik" src="https://github.com/user-attachments/assets/6f996750-75d0-4844-8fe0-a236ad0b9445" />
<br>
<br>

<br>
<img width="1155" height="552" alt="grafik" src="https://github.com/user-attachments/assets/e24a9d78-cfc1-461b-b546-d053be800df0" />
<br>
<br>

<br>
<img width="1260" height="925" alt="grafik" src="https://github.com/user-attachments/assets/168afdca-bc5a-43fe-aeab-a5b06bb1e0dc" />
<br>
<br>



# Settings

<br>
<img width="872" height="690" alt="grafik" src="https://github.com/user-attachments/assets/52cbde1e-e4ea-4eb4-82e6-e5bc8454c9cd" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/1af4979c-1a7a-461e-a4fd-8209893a5eb8" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/4d7da813-5483-47f8-b11b-88bb3ef4c4b0" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/47759978-996d-482b-ba0d-b4db02ebdd2a" />
<br>
<br>


<br>

# Important

Windows may show this message because the file was downloaded from the internet. Windows adds a security marker (“Mark of the Web”) to downloaded files, especially ZIP files or unsigned EXE files. This is normal Windows behavior and does not mean the application is malicious.

<br>
<img width="373" height="473" alt="grafik" src="https://github.com/user-attachments/assets/516a166c-11fc-425c-9847-8b06831c5ae3" />
<br>

## Core Features

- Scan drives or selected folders.
- Sort folders and files by size, percentage, and path.
- Switch between table view, pie chart, and bar chart.
- Use fast scan paths where available:
  - NTFS MFT scan for elevated NTFS fixed drives.
  - NT API directory scan as fallback.
  - Standard directory scan as final fallback.
- Show live scan progress with scanned folders, files, bytes, and skipped folders.
- Load cached scan data and verify changes on later scans.
- Export scan results to CSV.
- Copy export data to the clipboard.
- Open selected items in Windows Explorer.
- Optional Explorer context menu entry for folders and drives.
- Optional display of files inside the tree.
- Configurable layout (in progress):
  - Windows default
  - Windows light mode
  - Windows dark mode

## Differences to others

- Simpler than professional software: No enterprise reporting, scheduled scans, duplicate search, advanced file search, or broad remote-storage feature set.
- More focused: No treemap or extension statistics; instead it offers table, pie, and bar views.
- Faster-first design: tries NTFS MFT scanning, then NT API scanning, then standard scanning.
- Smaller scope: built for quick local Windows storage inspection, not full storage management.

## Special Notes

- Built with .NET 8 Windows Forms.
- Targets Windows x64.
- Uses a single-file publish setup.
- Uses per-user settings stored next to the application.
- Context menu registration is written under the current user registry hive.
- NTFS MFT scanning requires administrator rights and an NTFS fixed drive.
- Reparse points / junctions can be skipped.
- CSV export supports path, size in GB, size in MB, and maximum export depth.


## Acknowledgements

WTF is an independently developed disk usage analyzer.
Thanks to the open-source disk-usage-analyzer community, including WinDirStat, for showing how useful visual disk space analysis tools can be.
