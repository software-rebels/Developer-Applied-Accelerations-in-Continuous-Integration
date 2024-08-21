# Developer-Applied Accelerations in Continuous Integration

> [!NOTE]
> This replication package has been tested on amd64 architecture with Ubuntu 24.04 operating system.

## Data
The data files include the following:

### Dataset
This directory contains the dataset that we used for our study, which is [TODO: EXACT SIZE] GB in size. We recommend using Btrfs with the compression feature enabled, which reduces the footprint of the dataset to [TODO: EXACT SIZE] GB (compression is an optional file system feature and has no effect to the data). The artifact contains:

- The `mongodb` directory, which contains the CircleCI dataset in the MongoDB data format after filtering has been applied.
- The `postgresql` directory, which contains data after processing has been applied (e.g., clustered data). This data provides the input for the web app, so that the expensive clustering computation is only performed once.

### Inspection
This directory contains the results of our manual inspection. The artifact contains:

- The `inspection` directory, which includes `.xlsx` and `.csv` files. Within the `inspection` directory are the following files:
    - `k-means_all.csv`: The detected ratio of each build job with respect to the K-means clustering approach.
    - `k-means-with-rule.csv`:  The detected ratio of each build job with respect to the rule-based detection approach.
    - `KMeansCluster.csv`: K-means clustering data of each job on a monthly basis (including the mean and variance measurements).
    - `KMeansClusterRatio.csv`: The calculated ratio of each project.
    - `long-tail.csv`: The long-tail data. [NOTE: We need to explain what this is]
    - `rq1_inspection_agreed.csv` and `rq1_inspection.xlsx`: The inspection results of RQ1 in `.csv` and `.xlsx` formats.
    - `rq2_classification.xlsx` and `rq2_Final Labels.csv`: The results of the classification inspection of RQ2.
    - `ruled_detection.csv`: Results of rule-based detection approach.

### Code
This directory contains the tool that we developed to conduct this study, including a web app to visualize clustering results and the scripts that we used to compute results. The directory contains:

- The `compose.yml` file, which specifies the set of containers that are needed to run the codebase.
- The `src` directory, which includes the used .NET source code.

## Setup
1. **Preparing Operating System**: The following steps have been performed on a vanilla Ubuntu Server 24.04 system. Adjustments may be needed for other systems.
2. **Install and Configure Docker**: The recommended way is to use the script:
    ```sh
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo systemctl enable --now docker.service
    sudo usermod -aG docker $USER
    ```
    After executing these commands, logout of the machine and log back in. Verify this step by executing `docker ps -a`, which should produce output that looks like:
    ```
    CONTAINER ID   IMAGE           COMMAND                   CREATED        STATUS        PORTS     NAMES
    ```
    This output indicates that you have successfully installed Docker and currently there is no running container.
4. **Install .NET SDK**: Execute the following command:
    ```sh
    sudo apt update
    sudo apt install -y dotnet-sdk-8.0
    ```
5. **Prepare data files**: Place all of the files mentioned in the “Data” section to the same folder. <!-- [TODO: This is unclear. I do not know what files are being referenced here.] -->

## Usage
In this section, we describe how to use the data and code to reproduce the results that we present in our paper.

### RQ1
#### Manual Inspection
The samples and their inspection results are stored in the `rq1_inspection.xlsx` file. The `rq1_inspection_agreed.csv` file presents the same data in a format that is easier to process.

#### Evaluation
Change working directories to the `src/BuildAcceleration/BuildAcceleration` directory, and then execute `dotnet run -- -h`.

- To calculate clusters and ratios, execute the `dotnet run -- cluster` command (this requires the full [TODO: Size] GB dataset, which is available on request). [TODO: I don't think this is good enough any more. Did the UW library provide us with a way to share our large data set?]
- To detect accelerated jobs by rules, execute the `dotnet run -- detect` command (this also requires the dataset).

Note that the results of clustering `KMeansCluster.csv` and `KMeansClusterRatio.csv` are located in the replication directory. The detection results appear in the `ruled_detection.csv` file, which is also placed in the replication directory. [TODO: Please check this carefully, as I tried to clarify the text, but it was unclear to me what we were trying to say.]

### RQ2 
#### Detection
To detect accelerated jobs, execute the `dotnet run -- cluster --all` and `dotnet run -- detect --all` commands (requires dataset). These commands produce `k-means_all.csv` and `ruled_detection.csv`. Then, using a spreadsheet editor like Excel, select the detected jobs based on your preferred threshold.

#### Manual Inspection
The inspection results are stored in the `rq2_classification.xlsx` file. The `rq2_Final Labels.csv` file presents the same data in a format that is easier to process.

#### Processing (Converting labels to patterns)
As described in the paper, the labels are full sentences. To convert them to the pattern names, first, place the following files:

- `rq2_Final Labels.csv` to `Desktop/build_accel/Final Labels.csv`. [TODO: Why are these paths saying `Desktop`?]
- `KMeansClusterRatio.csv` to `Desktop/build_accel/KMeansClusterRatio.csv`.
- `rq1_inspection_agreed.csv` to `Desktop/build_accel/inspection_agreed.csv`.

Then, execute the `dotnet run -- count-categories` command. The results will be stored in the `Desktop/build_accel` directory. Note that we did further modifications to the category and pattern names, so there might be a very slight difference from the names in the paper. [TODO: This needs to be clarified or the script output needs to be updated to match the paper results.]
