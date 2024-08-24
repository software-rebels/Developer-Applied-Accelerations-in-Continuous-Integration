# Developer-Applied Accelerations in Continuous Integration

> [!NOTE]
> This replication package has been tested on amd64 architecture with Ubuntu 24.04 operating system.

## Data
The data files include the following:

### Dataset
This directory contains the dataset that we used for our study, which is 235 GB in size. We recommend using Btrfs with the compression feature enabled, which reduces the footprint of the dataset to 46 GB (compression is an optional file system feature and has no effect to the data). The artifact contains:

- The `mongodb` directory (220GB), which contains the CircleCI dataset in the MongoDB data format after filtering has been applied.
- The `postgresql` directory (15GB), which contains data after processing has been applied (e.g., clustered data). This data provides the input for the web app, so that the expensive clustering computation is only performed once.

> [!NOTE]
> The datasets (i.e., the `mongodb` and `postgresql` directories) are distributed separately, as a compressed format `.tar.zst`. Please download the files [here]() **TODO: upload file**

### Inspection
This directory contains the results of our manual inspection. The artifact contains:

- The `inspection` directory, which includes `.xlsx` and `.csv` files. Within the `inspection` directory are the following files:
    - `k-means_all.csv`: The detected ratio of each build job with respect to the K-means clustering approach.
    - `k-means-with-rule.csv`:  The detected ratio of each build job with respect to the rule-based detection approach.
    - `KMeansCluster.csv`: K-means clustering data of each job on a monthly basis (including the mean and variance measurements).
    - `KMeansClusterRatio.csv`: The calculated ratio of each project.
    - `long-tail.csv`: The long-tail data of Figure 2 in the paper.
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
4. **Prepare data files**: Clone the repository and download `mongodb.tar.zst` and `postgresql.tar.zst` to the repository folder. Then extract the `mongodb.tar.zst` and `postgresql.tar.zst` files.
    ```sh
    tar --zstd -xvf mongodb.tar.zst
    tar --zstd -xvf postgresql.tar.zst
    ```
    After extracting, you should have `mongodb` and `postgresql` folders.
5. **Launch database instances**: Run the following command to launch database instances
    ```sh
    docker compose up -d
    ```
    You should see the following output:
    ```
    ✔ Container dev-applied-accel-web    Running
    ✔ Container dev-applied-accel-mongo  Running
    ✔ Container dev-applied-accel-pg     Running
    ```

### Clean
When you finish using the dataset, run the command to stop:

```sh
docker compose down
```

## Usage
In this section, we describe how to use the data and code to reproduce the results that we present in our paper.

### Check Dataset (Optional)

If you want to peek the dataset, use any database software such as DataGrip or official command line tool.

- To check the build data in our dataset, connect to `mongodb://ip:27017/forecastBuildTime`, where `ip` is the IP address where you run the dataset.

  Leave user name blank as auth.
- To check our processed data, connect to `postgresql://ip:13339/forecasting`

  User name: postgres

  Password: Ie98Az0R2jjrNKHeEJFGtbRpxrZLN0xB

### Manual Inspection Results
#### RQ1
The samples and their inspection results are stored in the `rq1_inspection.xlsx` file. The `rq1_inspection_agreed.csv` file presents the same data in a format that is easier to process.

#### RQ2
The inspection results are stored in the `rq2_classification.xlsx` file. The `rq2_Final Labels.csv` file presents the same data in a format that is easier to process.

> [!NOTE]
> If you find any link in the file to `https://aws-forecast.b11p.com`, please replace the schema and host with `http://ip:8080`. We did not do this replacement in the file as the actual IP address is from your environment.

### Clustering
- To check the clustering result, go to the `KMeansClusters` table in the postgres data. The table contains the centers of the longer and shorter clusters.
- Also, use the following link to check data. Remember to replace `repo` and `job` parameters with what you want to query:

  <http://ip:8080/TimeSeries/ByCluster?repo=diem/diem&job=code_coverage> (Be sure to complete the setup steps and replace ip with your actual IP address)

  This link shows the build durations of each build (like Figure 3 in the paper). Only if there are data available: The lower and higher clusters are represented in different colors. The line represents the percentage of builds in the lower cluster of each month.

### Detection
The location of detection results `k-means_all.csv` and `ruled_detection.csv`. Use a spreadsheet editor like Excel to check the results of detection, including the ratio from clustering-based approach and the results from rule-based approach.

Note that the results of clustering `KMeansCluster.csv` and `KMeansClusterRatio.csv` are located in the replication directory. Please check the “Data-Inspection” section of this README to find explaination.
