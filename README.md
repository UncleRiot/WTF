# WTF – Where’s The Filespace

WTF is a lightweight Windows disk space analyzer for quickly finding where storage is used.
Multilingual support comming, when requested - (Now: English/German)

<br>
<img width="726" height="518" alt="grafik" src="https://github.com/user-attachments/assets/fbbd4af5-99ba-43e4-9cec-cd536f2566ed" />
<br>

# Contextmenu

<br>
<img width="186" height="356" alt="grafik" src="https://github.com/user-attachments/assets/15528f71-0df2-4df1-b081-22511a4267b6" />
<br>


# Export

<br>
<img width="426" height="317" alt="grafik" src="https://github.com/user-attachments/assets/700a4259-7e0a-44bf-b6e2-caabd360a37a" />
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

## Differences to TreeSize Professional and WinDirStat

- Simpler than TreeSize Professional: no enterprise reporting, scheduled scans, duplicate search, advanced file search, or broad remote-storage feature set.
- More focused than WinDirStat: no treemap or extension statistics; instead it offers table, pie, and bar views.
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

## Thanks

Thanks to WinDirStat and TreeSize for the inspiration and ideas behind practical disk space visualization and analysis tools.
