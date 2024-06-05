using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable 8618

namespace ForecastBuildTime.MongoDBModels
{
    public class AllCommitDetail
    {
        // Either ISO format or Unix timestamp (second)
        [BsonElement("committer_date")] public string CommitterDate { get; set; }

        [BsonElement("body")] public string Body { get; set; }

        [BsonElement("branch")] public string? Branch { get; set; }

        // See above
        [BsonElement("author_date")] public string AuthorDate { get; set; }

        [BsonElement("committer_email")] public string CommitterEmail { get; set; }

        [BsonElement("commit")] public string Commit { get; set; }

        [BsonElement("committer_login")] public string? CommitterLogin { get; set; }

        [BsonElement("committer_name")] public string CommitterName { get; set; }

        [BsonElement("subject")] public string Subject { get; set; }

        [BsonElement("commit_url")] public string CommitUrl { get; set; }

        [BsonElement("author_login")] public string? AuthorLogin { get; set; }

        [BsonElement("author_name")] public string AuthorName { get; set; }

        [BsonElement("author_email")] public string AuthorEmail { get; set; }
    }

    public class Canceler
    {
        [BsonElement("avatar_url")] public string AvatarUrl { get; set; }

        [BsonElement("external_id")] public int ExternalId { get; set; }

        [BsonElement("id")] public int CancelerId { get; set; }

        [BsonElement("name")] public string Name { get; set; }

        [BsonElement("user?")] public bool User { get; set; }

        [BsonElement("domain")] public string Domain { get; set; }

        [BsonElement("type")] public string Type { get; set; }

        [BsonElement("authorized?")] public bool Authorized { get; set; }

        [BsonElement("provider_id")] public string ProviderId { get; set; }

        [BsonElement("login")] public string Login { get; set; }
    }

    // [BsonIgnoreExtraElements]
    public class CircleYml
    {
        [BsonElement("string")] public string String { get; set; }

        [BsonElement("lethal")] public object? Lethal { get; set; }

        [BsonElement("errors")] public object? Errors { get; set; }
    }

    public class BuildMessage
    {
        [BsonElement("type")] public string Type { get; set; }

        [BsonElement("message")] public string Message { get; set; }

        [BsonElement("reason")] public string? Reason { get; set; }
    }

    public class Properties
    {
        [BsonElement("build_agent")] public string BuildAgent { get; set; }

        [BsonElement("executor")] public string Executor { get; set; }

        [BsonElement("nomad_ami")] public string NomadAmi { get; set; }

        [BsonElement("availability_zone")] public string AvailabilityZone { get; set; }

        [BsonElement("instance_id")] public string InstanceId { get; set; }

        [BsonElement("instance_ip")] public string InstanceIp { get; set; }
    }

    public class BuildAgent
    {
        [BsonElement("image")] public string Image { get; set; }

        [BsonElement("properties")] public Properties Properties { get; set; }
    }

    public class ResourceClass
    {
        [BsonElement("cpu")] public int Cpu { get; set; }

        [BsonElement("ram")] public int Ram { get; set; }

        [BsonElement("class")] public string Class { get; set; }

        [BsonElement("name")] public string Name { get; set; }
    }

    // [BsonIgnoreExtraElements]
    public class Picard
    {
        [BsonElement("build_agent")] public BuildAgent BuildAgent { get; set; }

        [BsonElement("resource_class")] public ResourceClass ResourceClass { get; set; }

        [BsonElement("executor")] public string Executor { get; set; }

        [BsonElement("ssh_servers")] public object? SshServers { get; set; }

        [BsonElement("used_features")] public object? UsedFeatures { get; set; }
    }

    public class Previous
    {
        [BsonElement("build_num")] public int BuildNum { get; set; }

        [BsonElement("status")] public string Status { get; set; }

        [BsonElement("build_time_millis")] public long BuildTimeMillis { get; set; }
    }

    public class PreviousSuccessfulBuild
    {
        [BsonElement("build_num")] public int BuildNum { get; set; }

        [BsonElement("status")] public string Status { get; set; }

        [BsonElement("build_time_millis")] public long BuildTimeMillis { get; set; }
    }

    public class PullRequest
    {
        [BsonElement("head_sha")] public string HeadSha { get; set; }

        [BsonElement("url")] public string Url { get; set; }
    }

    public class Action
    {
        [BsonElement("truncated")] public bool Truncated { get; set; }

        [BsonElement("index")] public int Index { get; set; }

        [BsonElement("parallel")] public bool Parallel { get; set; }

        [BsonElement("failed")] public bool? Failed { get; set; }

        [BsonElement("infrastructure_fail")] public bool? InfrastructureFail { get; set; }

        [BsonElement("name")] public string Name { get; set; }

        [BsonElement("bash_command")] public string? BashCommand { get; set; }

        [BsonElement("status")] public string Status { get; set; }

        [BsonElement("timedout")] public bool? Timedout { get; set; }

        [BsonElement("continue")] public object? Continue { get; set; }

        [BsonElement("end_time")] public DateTimeOffset? EndTime { get; set; }

        [BsonElement("type")] public string Type { get; set; }

        [BsonElement("allocation_id")] public string? AllocationId { get; set; }

        [BsonElement("output_url")] public string? OutputUrl { get; set; }

        [BsonElement("start_time")] public DateTimeOffset StartTime { get; set; }

        [BsonElement("background")] public bool Background { get; set; }

        [BsonElement("exit_code")] public int? ExitCode { get; set; }

        [BsonElement("insignificant")] public bool Insignificant { get; set; }

        [BsonElement("canceled")] public bool? Canceled { get; set; }

        [BsonElement("step")] public int Step { get; set; }

        [BsonElement("run_time_millis")] public int? RunTimeMillis { get; set; }

        [BsonElement("has_output")] public bool HasOutput { get; set; }

        [BsonElement("source")] public string? Source { get; set; }

        [BsonElement("truncation_len")] public string? TruncationLen { get; set; }
    }

    public class Step
    {
        [BsonElement("name")] public string Name { get; set; }

        [BsonElement("actions")] public List<Action> Actions { get; set; }
    }

    public class User
    {
        [BsonElement("is_user")] public bool IsUser { get; set; }

        [BsonElement("login")] public string? Login { get; set; }

        [BsonElement("avatar_url")] public string? AvatarUrl { get; set; }

        [BsonElement("name")] public string? Name { get; set; }

        [BsonElement("vcs_type")] public string? VcsType { get; set; }

        [BsonElement("id")] public int? UserId { get; set; }
    }

    public class Workflows
    {
        [BsonElement("job_name")] public string JobName { get; set; }

        [BsonElement("job_id")] public string JobId { get; set; }

        [BsonElement("workflow_id")] public string WorkflowId { get; set; }

        [BsonElement("workspace_id")] public string WorkspaceId { get; set; }

        [BsonElement("upstream_job_ids")] public List<string> UpstreamJobIds { get; set; }

        [BsonElement("upstream_concurrency_map")]
        public Dictionary<string, List<Guid>> UpstreamConcurrencyMap { get; set; }

        [BsonElement("workflow_name")] public string WorkflowName { get; set; }
    }

    public class SshUser
    {
        [BsonElement("github_id")] public int GithubId { get; set; }

        [BsonElement("login")] public string Login { get; set; }

        [BsonElement("_id")] public string Id { get; set; }

        [BsonElement("type")] public string? Type { get; set; }

        [BsonElement("external_id")] public int? ExternalId { get; set; }
    }

    public class BuildEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("build_url")] public string BuildUrl { get; set; }

        [BsonElement("all_commit_details")] public List<AllCommitDetail>? AllCommitDetails { get; set; }

        [BsonElement("all_commit_details_truncated")]
        public bool? AllCommitDetailsTruncated { get; set; }

        // See comment in AllCommitDetail
        [BsonElement("author_date")] public string? AuthorDate { get; set; }

        [BsonElement("author_email")] public string? AuthorEmail { get; set; }

        [BsonElement("author_name")] public string? AuthorName { get; set; }

        [BsonElement("body")] public string? Body { get; set; }

        [BsonElement("branch")] public string? Branch { get; set; }

        [BsonElement("build_num")] public int BuildNum { get; set; }

        [BsonElement("build_parameters")] public Dictionary<string, string>? BuildParameters { get; set; }

        [BsonElement("build_time_millis")] public long? BuildTimeMillis { get; set; }

        [BsonElement("canceled")] public bool Canceled { get; set; }

        [BsonElement("canceler")] public Canceler? Canceler { get; set; }

        [BsonElement("circle_yml")] public CircleYml? CircleYml { get; set; }

        // See comment above
        [BsonElement("committer_date")] public string? CommitterDate { get; set; }

        [BsonElement("committer_email")] public string? CommitterEmail { get; set; }

        [BsonElement("committer_name")] public string? CommitterName { get; set; }

        [BsonElement("compare")] public string? Compare { get; set; }

        [BsonElement("context_ids")] public List<string>? ContextIds { get; set; }

        [BsonElement("dont_build")] public string? DontBuild { get; set; }

        [BsonElement("fail_reason")] public string? FailReason { get; set; }

        [BsonElement("failed")] public bool? Failed { get; set; }

        [BsonElement("has_artifacts")] public bool? HasArtifacts { get; set; }

        [BsonElement("infrastructure_fail")] public bool InfrastructureFail { get; set; }

        [BsonElement("is_first_green_build")] public bool IsFirstGreenBuild { get; set; }

        [BsonElement("job_name")] public string? JobName { get; set; }

        [BsonElement("lifecycle")] public string? Lifecycle { get; set; }

        [BsonElement("messages")] public List<BuildMessage> Messages { get; set; }

        [BsonElement("no_dependency_cache")] public bool? NoDependencyCache { get; set; }

        [BsonElement("node")] public string? Node { get; set; }

        [BsonElement("oss")] public bool Oss { get; set; }

        [BsonElement("outcome")] public string? Outcome { get; set; }

        [BsonElement("owners")] public List<string> Owners { get; set; }

        // May be weird value e.g., 600493cde1d87f460fc378ed
        // 6004aa35e1d87f460fcf6b70
        // 6004aa35e1d87f460fcf6b72
        [BsonElement("parallel")] public int Parallel { get; set; }

        [BsonElement("picard")] public Picard? Picard { get; set; }

        [BsonElement("platform")] public string Platform { get; set; }

        [BsonElement("previous")] public Previous? Previous { get; set; }

        [BsonElement("previous_successful_build")]
        public PreviousSuccessfulBuild? PreviousSuccessfulBuild { get; set; }

        [BsonElement("pull_requests")] public List<PullRequest> PullRequests { get; set; }

        [BsonElement("queued_at")] public DateTimeOffset QueuedAt { get; set; }

        [BsonElement("reponame")] public string Reponame { get; set; }

        [BsonElement("retries")] public List<int>? Retries { get; set; }

        [BsonElement("retry_of")] public int? RetryOf { get; set; }

        [BsonElement("ssh_disabled")] public bool SshDisabled { get; set; }

        [BsonElement("ssh_users")] public List<SshUser> SshUsers { get; set; }

        [BsonElement("start_time")] public DateTimeOffset? StartTime { get; set; }

        [BsonElement("status")] public string Status { get; set; }

        [BsonElement("steps")] public List<Step> Steps { get; set; }

        [BsonElement("stop_time")] public DateTimeOffset? StopTime { get; set; }

        [BsonElement("subject")] public string? Subject { get; set; }

        [BsonElement("timedout")] public bool Timedout { get; set; }

        [BsonElement("usage_queued_at")] public DateTimeOffset? UsageQueuedAt { get; set; }

        [BsonElement("user")] public User User { get; set; }

        [BsonElement("username")] public string Username { get; set; }

        [BsonElement("vcs_revision")] public string VcsRevision { get; set; }

        [BsonElement("vcs_tag")] public string? VcsTag { get; set; }

        [BsonElement("vcs_type")] public string VcsType { get; set; }

        [BsonElement("vcs_url")] public string VcsUrl { get; set; }

        [BsonElement("why")] public string? Why { get; set; }

        [BsonElement("workflows")] public Workflows? Workflows { get; set; }
    }
}