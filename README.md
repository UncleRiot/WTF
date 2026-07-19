# WTF – Where’s The Filespace

WTF is a lightweight Windows disk space analyzer for quickly finding where storage is used.  
Multilingual support coming when requested — currently available in **English, German, and Spanish**.

---

> [!IMPORTANT]
> ## ⚠️ Fastest mode = run as Administrator
> **WTF shows its full strength when run as Administrator.**
>
> In this mode, WTF can use an **extremely fast MFT-based scan**, just like other tools in this category.
>
> Running without Administrator rights may result in:
> - slower scans
> - reduced access to some file system data
> - less accurate results in certain cases
>
> **See the Wiki for details.**

---


# Main Window

<br>
<img width="1234" height="902" alt="grafik" src="https://github.com/user-attachments/assets/3cdfcc45-f968-40e0-a074-052d9b0fd1c7" />
<br>
<br>

# Space History

<br>
<img width="1624" height="902" alt="grafik" src="https://github.com/user-attachments/assets/4a1bd69e-9f41-4ed3-bfeb-3c1bd1d15ed3" />
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
Default Languages: English, Spanish, German. New languagefiles addable.
Dark/Lightmode.

<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/c67e1d2c-1d43-4afd-beb8-8113391e30b1" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/eeb0c6b5-4111-4768-9bcd-5d81a368d39b" />
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/ba43608a-f04d-40ba-a724-62b5331cd485" />
<br>
<br>
SQlite file for Space-Comparisons. Highly optimized regarding details/occupied storage.
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/3c3dd1e0-edc2-4c3b-80ac-b2236320b149" />
<br>
<br>
Logging
<br>
<img width="522" height="493" alt="grafik" src="https://github.com/user-attachments/assets/74c51fc0-090d-482d-9b78-6a8c4103328a" />
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
- WTF goes beyond showing what is large: it helps you understand what changed between scans.
- It highlights new, modified, and deleted files, shows which folders have grown, and makes storage changes easy to trace.
- Its focus is a clear, local, user-friendly history of where your disk space went.

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
