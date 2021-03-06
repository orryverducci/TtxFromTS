Release Notes
=============

2.0
---

* **NEW:** Support for outputting teletext packets directly to standard output
* **NEW:** Support for outputting teletext packets to a web socket
* **NEW:** Support for outputting teletext packets as a T42 file
* **NEW:** Support for looped output
* **NEW:** A teletext service can now be selected using the teletext service ID
* **NEW:** Support for fastext page links that point to specific subcodes
* **NEW:** Support for page enhancement links
* **IMPROVED:** The code has been significantly refactored for improved readability
* **FIXED:** Out of bounds crash that occurred while decoding enhancement packets
* **FIXED:** Incorrect processing of header clock if it contained text
* **FIXED:** Selection of the initial page when setting the header template
* **FIXED:** Error when an invalid data unit length is encountered
* **FIXED:** Invalid packet warnings when teletext subtitle packets are encountered while subtitles are disabled
* **FIXED:** Null reference error triggered by some transport streams
* **FIXED:** Encrypted packet warnings on unencrypted services

1.2.1
-----

* **FIXED:** Invalid encoding of Hamming 24/18 protected packets has been fixed

1.2
---

* **NEW:** Level 2.5+ enhancements are now decoded and exported
* **NEW:** TOP data pages are now properly decoded and exported
* **IMPROVED:** Updates to pages are now processed and applied to pages
* **IMPROVED:** Packet X/25 is now decoded and exported
* **FIXED:** Subpages are now exported in the right order
* **FIXED:** Fastext link numbers are no longer corrupted on magazine 8
* **FIXED:** Services using parallel transmission are now decoded correctly again

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
