# PaperPilot

PaperPilot is an assistant for guided verification and automatic splitting of scanned high-volume documents.

A digitization assistant that simplifies the process of scanning and organizing large volumes of paper documents. With automatic detection of blank pages and embedded QR codes, PaperPilot provides a non-destructive preview environment where users can adjust parameters, rename documents, and visually verify the structure before finalizing output. It’s the ideal tool for turning well-sorted paper folders into clean digital files — efficiently and reliably.

1. Print Splitting Pages
2. Insert them into your paper stack (physical folder) after each complete document (letter, contract etc.)
3. duplex scan the whole batch with containing blank pages (e.g. empty backsides of a single paged document). Count the pages.
4. PaperPilot needs to know the folder (input) containing new scans.
---
5. PaperPilot automatically imports oldest PDF and analyzes blank pages and splitting pages.
6. Check the analyzed page types in the PDF preview.
7. Insert the name for the paper stack (physical folder). This will be prefixed for the splitted files.
8. After verification the PDF will be exported into a output folder as multiple PDF files.
9. PaperPilot deletes imported PDF from input folder and starts at point 5 with the new oldest PDF. 
