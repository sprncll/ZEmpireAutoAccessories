# Thesis figures

## Figure 3 — Iterative and Incremental Model

Three formats of the same drawing. Use whichever your document wants:

| File | Use |
| --- | --- |
| `Figure3-Iterative-and-Incremental.png` | Insert into Word. 3273 px wide, about 10.9 inches at 300 dpi. |
| `Figure3-Iterative-and-Incremental.pdf` | Vector. Stays sharp at any size; best if the paper is submitted as PDF. |
| `Figure3-Iterative-and-Incremental.svg` | The editable source. Open in Inkscape, Illustrator or a text editor to change wording. |

**Caption to use:**

> Figure 3. Iterative and Incremental Model followed in the development of the
> Z Empire Auto Accessories Shop Management System.

### Why this and not Waterfall

Waterfall passes through each phase once. This project did not. Measured from
the repository's own commit history on 23 October 2026:

- 159 of 214 source files (74%) were modified again in a later week.
- 104 of them (49%) were modified again in three or more separate weeks.
- Only 55 (26%) were written once and never returned to.
- The plate-number rule alone went through seven passes between 11 September
  and 1 October; the cut-size feature through roughly ten; the shade feature
  through six.
- 25 defects are documented in the development report, each one sending work
  back to an earlier phase.

The figures above are reproducible:

```
git log --format='%H %ad' --date=format:%Y-W%V --name-only --reverse
```

Section 12 of `ZEmpire-System-Development-Report.pdf` lists the increments, and
Appendix A of the same document lists every commit with its date.
