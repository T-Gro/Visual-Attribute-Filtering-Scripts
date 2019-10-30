# Visual-Attribute-Filtering-Scripts

This is a collection of pre-processing C# scripts which are relevant to offline activities related to discovery of latent visual attributes out of extracted visual descriptors and existing data coming out of KNN self-join
- Do basic ETL between different input and output formats
- Prepare data for other tasks like KNN Self-Join
- Parse Output of these tasks
- Visualise results in form of HTML files
- Run filtering and clustering pipeline on visual attributes
- Visualise attribtue candidates with their patches


## Usage

The resulting files are to be used:
- As input to other steps of the pipeline
- as .html files to visualise results
- as data files to run in [VADET Admin UI](https://github.com/T-Gro/VADET-Admin)

## Prerequisites

The solution requires Visual Studio 2017 or higher.
It uses standard C# Console Application programs and does not have any dedicated build or deployment steps.

## Testing
The suite uses NUnit test framework, see example usage at [Zoot label processing test](https://github.com/T-Gro/Visual-Attribute-Filtering-Scripts/blob/master/ZootBataLabelsProcessing/ZootLabelProcessingTests.cs)

## Data formats
Depending on the task, this suite offers multiple options to serialize data.
- For transfer to ther utilities such as the Knn-Join, .csv export and import is used
- For humans, .html export is available
- For efficient (both fast and small memory footprint), Google Protobug protocol is used
  -- This is handy in multip-step pipelines, when the same file should be read between multiple runs.
