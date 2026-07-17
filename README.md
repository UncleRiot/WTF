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
<img width="1437" height="948" alt="grafik" src="https://github.com/user-attachments/assets/502aaa18-e8ed-4899-b9e6-a30e9ebd7928" />
<br>
<br>


# Pie-Chart / Tableview

<br>
<img width="1158" height="819" alt="grafik" src="https://github.com/user-attachments/assets/28fd3c50-7257-4b54-b165-c741895820f0" />
<br>
<img width="1158" height="819" alt="grafik" src="https://github.com/user-attachments/assets/eb672365-138a-4878-8db2-55b751f54540" />
<br>
<br>


# Settings

<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/50b8a0f6-6b0b-446f-9e8c-d1cacd29f1c8" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/902de9a5-8013-4d83-b5cf-c528501e26f7" />
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
