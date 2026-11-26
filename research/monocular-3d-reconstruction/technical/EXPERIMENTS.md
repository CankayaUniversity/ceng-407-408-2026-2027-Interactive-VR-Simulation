# Experiment ledger

Aggregate observations from actual local metric records. Only authorized equipment-only image derivatives accompany these measurements; raw captures and geometry are not distributed.

## LingBot execution

| Recording / frames | Inference s | Total s | Reserved GiB | Depth-derived points |
|---|---:|---:|---:|---:|
| V1 /32 | 9.38 | 84.93 | 8.646 | 487,259 |
| V1 /128 | 36.10 | 158.77 | 8.688 | 1,938,221 |
| V2 /128 | 38.32 | 213.86 | 8.688 | 1,947,065 |

## Matched Gaussian runs

| Cohort / method | Train / held-out | PSNR dB | SSIM | Training s | Total s | Reserved GiB |
|---|---:|---:|---:|---:|---:|---:|
| V1 / A | 33 / 5 | 15.60 | 0.581 | 41.05 | 45.85 | 0.217 |
| V1 / B | 33 / 5 | 18.18 | 0.690 | 39.91 | 44.44 | 0.240 |
| V2 / A | 32 / 6 | 12.90 | 0.542 | 39.36 | 43.67 | 0.221 |
| V2 / B | 32 / 6 | 14.67 | 0.605 | 38.82 | 43.09 | 0.221 |

Different views/cohorts preclude a controlled cross-recording geometry comparison. Camera estimation may see held-out RGB.

## V2 registration

| Sampling / matching | Largest model | Fraction | Stage s | Sparse points | Reprojection px |
|---|---:|---:|---:|---:|---:|
| 128 / exhaustive extension | 38/128 | 29.7% | 59.61 | 1,536 | 0.678 |
| 364 / sequential | 127/364 | 34.9% | 91.13 | 7,882 | 0.647 |
| 364 / blur-aware sequential | 115/364 | 31.6% | 98.29 | 8,959 | 0.587 |
| 364 / exhaustive extension | 235/364 | 64.6% | 441.49 | 18,008 | 0.732 |

Exhaustive timings are additional to sequential extraction/matching. Dense:91.13+441.49=532.62s; preparation56.45s. Previous128:26.54+59.61=86.16s.
Same128 control:38→82 registered. No Gaussian training on the235-camera result.

## Evidence boundary

Arrays, inverses, rotations, camera names and disk counts were checked; this is not physical ground truth.
Source records remain in the POC. evidence-sources.json records hashes and logical record names. Results.json contains explicit aggregate-field allowlists, no raw poses or private images.
