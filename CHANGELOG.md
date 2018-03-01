Release Notes
=============

1.2
---

* **IMPROVED:** Updates to pages are now processed and applied to pages
* **FIXED:** Subpages are now exported in the right order

1.1
---

* **NEW:** Creates a vbit2 configuration file with the header and broadcast service data from the decoded service
* **NEW:** Option to set cycle time between subpages
* **NEW:** Option to specify the directory to export files to
* **NEW:** Option to include subtitle pages in exported pages
* **IMPROVED:** Arguments provided in the wrong format are now handled more gracefully
* **IMPROVED:** Serially transmitted services are now fully supported
* **IMPROVED:** An exit code is now returned when an error has occurred
* **FIXED:** Subpages within a page are now exported in a single TTI file, resolving an issue with vbit2
* **FIXED:** TOP data pages and pages with subcode 3F7F are now decoded and outputted

1.0
---

Initial release