# Thesis figures — Figure 3 (methodology)

The paper currently carries **"Figure 3: Waterfall Methodology"**. Waterfall
passes through each phase once, and this project did not. Two replacements are
provided; which one to use depends on how much of the chapter can still change.

**Do not swap the image on its own.** The prose around Figure 3 describes the
methodology too. A figure that says one thing and a paragraph that says another
is worse than either on its own.

## Option B — Iterative Waterfall  *(recommended for a final revision)*

`Figure3-OptionB-Iterative-Waterfall.{png,svg,pdf}`

The same six phases, the same colours and the same chevron style as the figure
already in the paper, with the feedback arrows added. A drop-in replacement.

- Caption: *Figure 3: Iterative Waterfall Methodology*
- Chapter edit: two or three sentences saying that a phase may be re-entered
  when testing or a review finds a defect.
- Academically sound: Royce's 1970 paper, which the waterfall model is taken
  from, included feedback between adjacent phases. The one-pass "pure"
  waterfall is a later simplification of it.

## Option C — Iterative and Incremental

`Figure3-OptionC-Iterative-and-Incremental.{png,svg,pdf}`

Two panels: (a) the phases repeated within an increment, (b) the seven
increments delivered over the project, with their real names and dates.

- Caption: *Figure 3: Iterative and Incremental Model*
- Chapter edit: the methodology section rewritten around increments.
- The most accurate description of what happened, and the bigger edit.

Note: panel (a) names its phases Requirements / Design / Implementation /
Testing / Review. If the chapter keeps the six-phase vocabulary already in the
paper, the SVG should be edited to match before use.

## Formats

| Extension | Use |
| --- | --- |
| `.png` | Insert into Word. 300 dpi. |
| `.pdf` | Vector; best if the paper is submitted as a PDF. |
| `.svg` | Editable source — open in Inkscape, Illustrator, or a text editor. |

## The evidence behind the change

Measured from the repository's commit history on 23 October 2026:

- 159 of 214 source files (74%) were modified again in a later week.
- 104 of them (49%) were modified again in three or more separate weeks.
- Only 55 (26%) were written once and never returned to.
- The plate-number rule went through seven passes between 11 September and
  1 October; the cut-size feature through roughly ten; the shade feature six.
- 25 defects are documented in the development report, each sending work back
  to an earlier phase.

Reproduce with:

```
git log --format='%H %ad' --date=format:%Y-W%V --name-only --reverse
```

Section 12 of `ZEmpire-System-Development-Report.pdf` lists the increments;
Appendix A of the same document lists every commit with its date. If that
report is submitted or shown at the defence, it is evidence against a
one-pass waterfall, which is the reason this matters.
