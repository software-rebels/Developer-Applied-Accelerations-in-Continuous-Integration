# Developer-Applied Accelerations in Continuous Integration

> [!NOTE]
> This replication package has been tested on amd64 architecture with Ubuntu 24.04 operating system.

## Data
The data files include the following:

### Dataset
We publish the dataset we used for our study. The data occupy nearly 300GB space. We recommend to use Btrfs and enable compression feature, so that the data only occupy about 100GB.

- `mongodb` folder. This folder contains the CircleCI dataset, after filtering. The content in this folder is in MongoDB data format, instead of bson dump format.
- `postgresql` folder. This folder contains processed data, such as the clustering data. It is used by the web app, so that the expensive clustering computation does not have to be executed on each web request.

### Inspection
We publish the results of our manual inspection.

- `inspection` folder, including the “.xlsx” files and “.csv” files.
    - `k-means_all.csv`: The detected ratio of each build job in the K-means approach.
    - `k-means-with-rule.csv`: Same as above but with rule-based detection applied.
    - `KMeansCluster.csv`: Each month's K-means clustering data of each job, including the mean and variances.
    - `KMeansClusterRatio.csv`: The calculated ratio of each project.
    - `long-tail.csv`: The long-tail data.
    - `rq1_inspection_agreed.csv`: The RQ1 inspection results, converted to csv for convenient process.
    - `rq1_inspection.xlsx`: The RQ1 inspection results.
    - `rq2_classification.xlsx`: The RQ2 classification inspection results.
    - `rq2_Final Labels.csv`: The RQ2 classification inspection results, converted to csv for convenient process.
    - `ruled_detection.csv`: Results of rule-based detection.

### Code
We publish our tool for this study, including a simple web app to show clustering, the tool we used to generate results, etc.

- `compose.yml`. this file is used to create containers.
- `src` folder, including the used .NET source code.

## Setup

1. **Preparing Operating System**: The following steps have been tested on a venilla Ubuntu Server 24.04 system. If you use other operating systems, be aware that your environment may differ from the authors', so adjust the relative command when necessary.
2. **Install Docker and grant access to Docker**; Make sure you have permission to run `sudo`. The recommended way is to use the script:
    ```sh
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo systemctl enable --now docker.service
    sudo usermod -aG docker $USER
    ```
    After doing this, logout and login again. Verify this step by running `docker ps -a`.
3. **Install .NET SDK**: Run command:
    ```sh
    sudo apt update
    sudo apt install -y dotnet-sdk-8.0
    ```
4. **Prepare data files**: Put all files mentioned above in the same folder.

## Usage
Next, we present how to use the published data and code to reproduce results in the paper.

### RQ1
#### Manual Inspection
The samples and inspection results are in the `rq1_inspection.xlsx` file. The file `rq1_inspection_agreed.csv` is for computer processing (evaluating performance, counting agreement, etc.).

#### Evaluation
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