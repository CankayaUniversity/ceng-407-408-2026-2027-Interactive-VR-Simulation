# Project Documentation

Official repository: [CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation](https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation).

## CENG 407 Academic Deliverables

- Literature and Technology Review — 15 November 2026: [Submission PDF](LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) · [Editable Markdown](LITERATURE_AND_TECHNOLOGY_REVIEW.md). English report covering introductory nursing simulation, instructor authoring, photograph/video reconstruction, local and online AI processing, desktop/VR technologies, comparison tables, evaluation direction, and 31 references.

## Application Documentation

- [Desktop application setup and use](DESKTOP_APP.md)
- [Desktop architecture](DESKTOP_ARCHITECTURE.md)
- [Nursing Skills Room and equipment interaction](NURSING_ROOM.md)
- [Nursing room validation](NURSING_ROOM_VALIDATION_2026_11_14.md)
- [Desktop migration changes](CHANGES_UNITY_DESKTOP.md)
- [University repository preparation and current changes](CHANGES_UNIVERSITY_NURSING_2026_11_15.md)

The academic report describes the intended graduation-project scope and distinguishes it from the current implementation. Application documents describe the corresponding implemented prototype and its validation limits.

The report cover includes the university, department, six team members and student
numbers, and supervisor. Cover data is maintained in [report metadata](report-metadata.json).
The repeatable PDF builder is [tools/build_literature_review.py](tools/build_literature_review.py)
(Python 3, ReportLab, and Windows Times New Roman/Calibri fonts). Run it from the
repository root with `python docs/tools/build_literature_review.py`. The university
mark and its source note are in [assets](assets/SOURCES.md).
