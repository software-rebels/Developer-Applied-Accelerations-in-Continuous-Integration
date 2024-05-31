# Developer-Defined Accelerations in Continuous Integration: A Detection Approach and Catalog of Patterns
## Replication
Our dataset is very large (about 1TB uncompressed, ~100GB compressed). We did not find a hosting service which can host such large files while keep anonymous during the review process. Hence, our dataset is available on request. This repository contains the inspection results.

### RQ1
#### Manual Inspection
The samples and inspection results are in the `rq1_inspection.xlsx` file. The file `rq1_inspection_agreed.csv` is for computer processing (evaluating performance, counting agreement, etc.).

#### Evaluation
First, download the files in `src` folder. Then, install [.NET SDK](https://dotnet.microsoft.com/en-us/download) (.NET 8.0 is preferred).

Locate to `src/BuildAcceleration/BuildAcceleration` folder, and run `dotnet run -- -h`.

- To calculate clusters and ratios, run `dotnet run -- cluster` (this requires the dataset, which is available on request).
- To detect accelerated jobs by rules, run `dotnet run -- detect` (this also requires the dataset).

However, the results of clustering `KMeansCluster.csv` and `KMeansClusterRatio.csv` are placed in the replication folder. The results of detection `ruled_detection.csv` is also placed.

### RQ2 
#### Detection
To detect accelerated jobs, run `dotnet run -- cluster --all` and `dotnet run -- detect --all` (requires dataset). This produces `k-means_all.csv` and `ruled_detection.csv`. Then manually (using Excel) select detected jobs based on the threshold.

#### Manual Inspection
The inspection results are in the `rq2_classification.xlsx` file. The file `rq2_Final Labels.csv` is for computer processing.

#### Processing (Converting labels to patterns)
As described in the paper, the labels are full sentences. To convert it to pattern names, first, put the following files:

- `rq2_Final Labels.csv` to `Desktop/build_accel/Final Labels.csv`.
- `KMeansClusterRatio.csv` to `Desktop/build_accel/KMeansClusterRatio.csv`.
- `rq1_inspection_agreed.csv` to `Desktop/build_accel/inspection_agreed.csv`.

Then, run `dotnet run -- count-categories`. The results will appear in `Desktop/build_accel` folder. Note that we did further modifications to the category and pattern names, so there might be a very slight difference from the names in the paper.