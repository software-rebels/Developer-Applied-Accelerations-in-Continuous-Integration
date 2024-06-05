using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForecastBuildTime.SqlModels;

public class TravisBuild
{
    [Column("tr_build_id")]
    public int TrBuildId { get; set; }

    [Column("gh_project_name")]
    public string GhProjectName { get; set; } = default!;

    [Column("gh_is_pr")]
    public bool GhIsPr { get; set; }

    [Column("gh_pr_created_at")]
    public DateTimeOffset GhPrCreatedAt { get; set; }

    [Column("gh_pull_req_num")]
    public int? GhPullReqNum { get; set; }

    [Column("gh_lang")]
    public string GhLang { get; set; } = default!;

    [Column("git_merged_with")]
    public string? GitMergedWith { get; set; }

    [Column("git_branch")]
    public string GitBranch { get; set; } = default!;

    [Column("gh_num_commits_in_push")]
    public int? GhNumCommitsInPush { get; set; }

    [Column("gh_commits_in_push")]
    public string? GhCommitsInPush { get; set; }

    [Column("git_prev_commit_resolution_status")]
    public string GitPrevCommitResolutionStatus { get; set; } = default!;

    [Column("git_prev_built_commit")]
    public string? GitPrevBuiltCommit { get; set; }

    [Column("tr_prev_build")]
    public int? TrPrevBuild { get; set; }

    [Column("gh_first_commit_created_at")]
    public DateTimeOffset GhFirstCommitCreatedAt { get; set; }

    [Column("gh_team_size")]
    public int GhTeamSize { get; set; }

    [Column("git_all_built_commits")]
    public string GitAllBuiltCommits { get; set; } = default!;

    [Column("git_num_all_built_commits")]
    public int GitNumAllBuiltCommits { get; set; }

    [Column("git_trigger_commit")]
    public string GitTriggerCommit { get; set; } = default!;

    [Column("tr_virtual_merged_into")]
    public string? TrVirtualMergedInto { get; set; }

    [Column("tr_original_commit")]
    public string TrOriginalCommit { get; set; } = default!;

    [Column("gh_num_issue_comments")]
    public int? GhNumIssueComments { get; set; }

    [Column("gh_num_commit_comments")]
    public int GhNumCommitComments { get; set; }

    [Column("gh_num_pr_comments")]
    public int? GhNumPrComments { get; set; }

    [Column("git_diff_src_churn")]
    public int GitDiffSrcChurn { get; set; }

    [Column("git_diff_test_churn")]
    public int GitDiffTestChurn { get; set; }

    [Column("gh_diff_files_added")]
    public int GhDiffFilesAdded { get; set; }

    [Column("gh_diff_files_deleted")]
    public int GhDiffFilesDeleted { get; set; }

    [Column("gh_diff_files_modified")]
    public int GhDiffFilesModified { get; set; }

    [Column("gh_diff_tests_added")]
    public int GhDiffTestsAdded { get; set; }

    [Column("gh_diff_tests_deleted")]
    public int GhDiffTestsDeleted { get; set; }

    [Column("gh_diff_src_files")]
    public int GhDiffSrcFiles { get; set; }

    [Column("gh_diff_doc_files")]
    public int GhDiffDocFiles { get; set; }

    [Column("gh_diff_other_files")]
    public int GhDiffOtherFiles { get; set; }

    [Column("gh_num_commits_on_files_touched")]
    public int GhNumCommitsOnFilesTouched { get; set; }

    [Column("gh_sloc")]
    public int GhSloc { get; set; }

    [Column("gh_test_lines_per_kloc")]
    public double GhTestLinesPerKloc { get; set; }

    [Column("gh_test_cases_per_kloc")]
    public double GhTestCasesPerKloc { get; set; }

    [Column("gh_asserts_cases_per_kloc")]
    public double GhAssertsCasesPerKloc { get; set; }

    [Column("gh_by_core_team_member")]
    public int GhByCoreTeamMember { get; set; }

    [Column("gh_description_complexity")]
    public int? GhDescriptionComplexity { get; set; }

    [Column("gh_pushed_at")]
    public DateTimeOffset? GhPushedAt { get; set; }

    [Column("gh_build_started_at")]
    public DateTimeOffset GhBuildStartedAt { get; set; }

    [Column("tr_status")]
    public string TrStatus { get; set; } = default!;

    [Column("tr_duration")]
    public int TrDuration { get; set; }

    [Column("tr_jobs")]
    public string TrJobs { get; set; } = default!;

    [Column("tr_build_number")]
    public int TrBuildNumber { get; set; }

    [Column("tr_job_id")]
    public int TrJobId { get; set; }

    [Column("tr_log_lan")]
    public string TrLogLan { get; set; } = default!;

    [Column("tr_log_status")]
    public string TrLogStatus { get; set; } = default!;

    [Column("tr_log_setup_time")]
    public int? TrLogSetupTime { get; set; }

    [Column("tr_log_analyzer")]
    public string TrLogAnalyzer { get; set; } = default!;

    [Column("tr_log_frameworks")]
    public string? TrLogFrameworks { get; set; }

    [Column("tr_log_bool_tests_ran")]
    public bool TrLogBoolTestsRan { get; set; }

    [Column("tr_log_bool_tests_failed")]
    public bool? TrLogBoolTestsFailed { get; set; }

    [Column("tr_log_num_tests_ok")]
    public int? TrLogNumTestsOk { get; set; }

    [Column("tr_log_num_tests_failed")]
    public int? TrLogNumTestsFailed { get; set; }

    [Column("tr_log_num_tests_run")]
    public int? TrLogNumTestsRun { get; set; }

    [Column("tr_log_num_tests_skipped")]
    public int? TrLogNumTestsSkipped { get; set; }

    [Column("tr_log_tests_failed")]
    public string? TrLogTestsFailed { get; set; }

    [Column("tr_log_testduration")]
    public double? TrLogTestduration { get; set; }

    [Column("tr_log_buildduration")]
    public double? TrLogBuildduration { get; set; }

    [Column("build_successful")]
    public bool BuildSuccessful { get; set; }
}
