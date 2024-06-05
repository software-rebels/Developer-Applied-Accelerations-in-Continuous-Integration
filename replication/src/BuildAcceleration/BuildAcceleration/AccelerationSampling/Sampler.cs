using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.AccelerationSampling;

public class Sampler
{
    private readonly ForecastingContext _forecastingContext;
    private readonly ILogger<Sampler> _logger;

    public Sampler(ForecastingContext forecastingContext, ILogger<Sampler> logger)
    {
        _forecastingContext = forecastingContext;
        _logger = logger;
    }

    public async ValueTask SampleAsync()
    {
        int onePer = 100;
        var jobs = await _forecastingContext.Builds.AsNoTracking()
            .Select(b => new { b.VcsUrl, b.JobName })
            .Distinct()
            .ToListAsync().ConfigureAwait(false);
        double sampleCount = jobs.Count / (double)onePer;
        var jobCounts = new List<(string vcsUrl, string jobName, int count)>();
        int totalCount = 0;
        foreach (var job in jobs)
        {
            var count = await _forecastingContext.Builds.CountAsync(b => b.VcsUrl == job.VcsUrl && b.JobName == job.JobName).ConfigureAwait(false);
            totalCount += count;
            jobCounts.Add((job.VcsUrl, job.JobName, count));
        }
        decimal interval = (long)totalCount * onePer / (decimal)jobs.Count;
        decimal current = (decimal)(new Random().NextDouble()) * interval;
        decimal offset = 0;
        Shuffle(jobCounts, new Random());
        var samples = new List<(string vcsUrl, string jobName, int count)>();
        foreach (var job in jobCounts)
        {
            offset += job.count;
            if (offset > current)
            {
                samples.Add(job);
                current += interval;
            }
        }

        foreach (var s in samples)
        {
            var evals = await _forecastingContext.Evaluations.AsNoTracking().Where(e => e.VcsUrl == s.vcsUrl && e.JobName == s.jobName).ToListAsync().ConfigureAwait(false);
            var sample = new AccelerationSample
            {
                VcsUrl = s.vcsUrl,
                JobName = s.jobName,
                BuildCount = s.count,
                MmreAverage = evals.First(e => e.Approach == "simple-average")?.MMRE ?? double.NaN,
                MmreLinearRegression = evals.First(e => e.Approach == "simple-linear-regression")?.MMRE ?? double.NaN,
                MmreSlidingWindow = evals.First(e => e.Approach == "window-size-average-30-days")?.MMRE ?? double.NaN,
            };
            _forecastingContext.AccelerationSamples.Add(sample);
        }
        await _forecastingContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask AddMissingSamplesAsync((string vcsUrl, string jobName)[] jobs = default!)
    {
        if (jobs is null)
        {
            string hardcode = @"https://github.com/envoyproxy/envoy	format
https://github.com/qlik-oss/after-work.js	test-protractor
https://github.com/apollographql/apollo-link	Apollo Link Schema
https://github.com/facebook/jest	test-or-deploy-website
https://github.com/pytorch/vision	binary_linux_wheel_py3.8_cpu
https://github.com/rubocop-hq/rubocop	jruby-9.2-ascii_spec
https://github.com/facebook/buck	macos_test_android_ndk_16
https://github.com/googleapis/gapic-generator-typescript	lint
https://github.com/rubocop-hq/rubocop	ruby-2.2-spec
https://github.com/cloudify-cosmo/cloudify-manager	test_cloudify_types
https://github.com/PyTorchLightning/pytorch-lightning	PyTorch-v1.4
https://github.com/cahirwpz/mimiker	build_mips_gcc
https://github.com/cri-o/cri-o	vendor
https://github.com/facebook/react	yarn_build
https://github.com/zeit/now-cli	test-integration-macos
https://github.com/expo/expo	home
https://github.com/apollographql/react-apollo	Node.js 8
https://github.com/entropic-dev/entropic	cli
https://github.com/influxdata/telegraf	test-go-1.11-386
https://github.com/mui-org/material-ui	test_material-ui-x
https://github.com/syuilo/misskey	ok
https://github.com/qlik-oss/after-work.js	lint
https://github.com/bazelbuild/rules_nodejs	test_e2e_legacy
https://github.com/angular/angular	test_aio_tools
https://github.com/facebookresearch/ParlAI	unittests_gpu12
https://github.com/fastlane/fastlane	Execute tests on macOS (Xcode 9.4.1, Ruby 2.4)
https://github.com/gatsbyjs/gatsby	integration_tests_cache_resilience
https://github.com/SFDO-Tooling/CumulusCI	test_robot
https://github.com/Rise-Vision/rise-vision-apps	e2e_template_editor
https://github.com/elementor/elementor	php72-wp48-build
https://github.com/os-autoinst/openQA	scheduler
https://github.com/raiden-network/raiden	smoketest-parity-development-3.7-matrix
https://github.com/api-platform/core	merge-and-upload-coverage
https://github.com/mozilla/addons-server	build-deploy
https://github.com/neomake/neomake	nvim-master
https://github.com/facebook/buck	mac_build_openjdk8
https://github.com/demisto/content	Server 4_1
https://github.com/ethereum/solidity	b_ubu_cxx17
https://github.com/plotly/plotly.py	python-2-7-orca
https://github.com/apollographql/apollo-client	Apollo Boost
https://github.com/spotify/scio	build_scalafix_rules_212
https://github.com/samvera/hyrax	build
https://github.com/lucas-clemente/quic-go	build-go1.10
https://github.com/pytorch/vision	binary_linux_conda_py3.6_cu110
https://github.com/nullworks/cathook	build
https://github.com/PowerDNS/pdns	build-recursor
https://github.com/OSGeo/homebrew-osgeo4mac	update-homebrew
https://github.com/facebook/buck	linux_test_heavy_integration
https://github.com/electron/electron	electron-linux-arm64-debug
https://github.com/grafana/grafana	build-enterprise-backend-linux-x64-musl
https://github.com/storybookjs/storybook	install-examples-deps
https://github.com/apollographql/apollo-server	NodeJS 10
https://github.com/pytorch/vision	binary_linux_wheel_py3.8_cu92
https://github.com/grafana/grafana	build-all-enterprise
https://github.com/artsy/emission	build-and-test
https://github.com/ENCODE-DCC/encoded	indexing
https://github.com/rakudo/rakudo	test-macos-1
https://github.com/simdjson/simdjson	clang-avx-google-benchmarks
https://github.com/alteryx/featuretools	py38 windows unit tests
https://github.com/home-assistant/core	test 3.6
https://github.com/mozilla/bedrock	test_py
https://github.com/mozilla/addons-server	integration_test
https://github.com/simdjson/simdjson	clang-avx-threaded
https://github.com/Automattic/wp-calypso	danger
https://github.com/Automattic/wp-calypso	setup-1
https://github.com/cBioPortal/cbioportal-frontend	check_incorrect_import_statements
https://github.com/citusdata/citus	test-13_check-follower-cluster
https://github.com/fishtown-analytics/dbt	integration-postgres-py38
https://github.com/cloudify-cosmo/cloudify-manager	test_clientv2_1_endpoints
https://github.com/fishtown-analytics/dbt	integration-postgres-py36
https://github.com/cuberite/cuberite	clang-tidy
https://github.com/glpi-project/glpi	php_7_3_test_suite
https://github.com/angular/angular	legacy-unit-tests-local
https://github.com/zulip/zulip	trusty-backend-python3.4
https://github.com/demisto/content	Server Master
https://github.com/expo/expo	babel_preset
https://github.com/facebook/react	test_build_prod
https://github.com/spotify/scio	build_212
https://github.com/activeadmin/activeadmin	testapp60
https://github.com/all-of-us/workbench	api-bigquery-test
https://github.com/apollographql/react-apollo	Bundlesize
https://github.com/facebook/react	test_source_www_experimental
https://github.com/tootsuite/mastodon	test-ruby2.5
https://github.com/pytorch/vision	binary_macos_wheel_py3.8_cpu
https://github.com/cucumber/cucumber	messages-javascript
https://github.com/cahirwpz/mimiker	kernel_tests_mips_clang
https://github.com/thought-machine/please	build-freebsd
https://github.com/angular/components	view_engine_snapshot_test_cronjob
https://github.com/hawkrives/gobbldygook	prettier
https://github.com/pingcap/docs-cn	lint
https://github.com/Automattic/wp-calypso	typecheck-strict
https://github.com/cBioPortal/cbioportal	pull_frontend_codebase
https://github.com/reasonml/reason	4.06.0
https://github.com/ethereum/solidity	t_osx_soltest
https://github.com/jumanjihouse/docker-aws	build-2
https://github.com/citahub/cita	Check Basic
https://github.com/apollographql/apollo-client	Apollo InMemory Cache
https://github.com/ngrx/platform	docs-tests
https://github.com/hashicorp/consul	go-test-sdk
https://github.com/aeternity/aeternity	eunit_minerva
https://github.com/robolectric/robolectric	test_3
https://github.com/selfrefactor/rambda	node-middle
https://github.com/angular/components	mdc_snapshot_test_cronjob
https://github.com/plotly/plotly.py	python-3-7-orca
https://github.com/googleapis/gapic-showcase	python-smoke-test
https://github.com/cloudify-cosmo/cloudify-manager	py3_compat
https://github.com/ant-design/ant-design	test_dist_15
https://github.com/diem/diem	code_coverage
https://github.com/PyTorchLightning/pytorch-lightning	cleanup-gke-jobs
https://github.com/cucumber/cucumber	tag-expressions-java
https://github.com/electron/electron	electron-linux-arm64
https://github.com/facebookresearch/ParlAI	unittests_38
https://github.com/robolectric/robolectric	test_4
https://github.com/hashicorp/consul	ember-build
https://github.com/redcanaryco/atomic-red-team	validate_atomics_generate_docs
https://github.com/AusDTO/dto-digitalmarketplace-frontend	check_styles
https://github.com/apollographql/apollo-link	Apollo Link Polling
https://github.com/hashicorp/vault	netbsd_amd64_package
https://github.com/angular/angular	build-ivy-npm-packages
https://github.com/angular/angular	setup
https://github.com/rubocop-hq/rubocop	ruby-2.3-rubocop
https://github.com/gatsbyjs/gatsby	e2e_tests_gatsby-static-image
https://github.com/reactioncommerce/reaction	snyk-security
https://github.com/heroku/cli	node12-test
https://github.com/Azure/acs-engine	k8s-windows-1.8-release-e2e
https://github.com/badges/shields	integration@node-14
https://github.com/palantir/conjure-java-runtime	compile
https://github.com/square/okhttp	checkjdk8
https://github.com/PowerDNS/pdns	test-recursor-api
https://github.com/babel/babel	build-standalone
https://github.com/citahub/cita	Check Contracts
https://github.com/envoyproxy/envoy	clang_tidy
https://github.com/pingcap/docs-cn	build
https://github.com/decaffeinate/decaffeinate	node-v10-latest
https://github.com/KissKissBankBank/kitten	build_and_test
https://github.com/diem/diem	terraform
https://github.com/grafana/metrictank	test
https://github.com/tootsuite/mastodon	build
https://github.com/hashicorp/consul	nomad-integration-master
https://github.com/hashicorp/consul	envoy-integration-test-1.8.0
https://github.com/activeadmin/activeadmin	ruby26rails60turbolinks
https://github.com/electron/electron	electron-linux-arm64-testing
https://github.com/LLK/scratch-gui	build
https://github.com/prometheus/prometheus	build
https://github.com/storybookjs/storybook	react-native
https://github.com/cucumber/cucumber	create-meta-java
https://github.com/cloudify-cosmo/cloudify-manager	test_premium
https://github.com/angular/components	snapshot_tests_local_browsers
https://github.com/storybookjs/storybook	install-e2e-deps
https://github.com/aeternity/aeternity	windows_package
https://github.com/spotify/scio	build_212-jdk11
https://github.com/Azure/acs-engine	test
https://github.com/scalyr/scalyr-agent-2	unittest-37
https://github.com/SFDO-Tooling/CumulusCI	lint
https://github.com/decidim/decidim	surveys
https://github.com/cucumber/cucumber	tag-expressions-javascript
https://github.com/pytorch/vision	binary_linux_conda_py3.5_cu92
https://github.com/RocketChat/Rocket.Chat.Android	build-kotlin-sdk
https://github.com/cBioPortal/cbioportal-frontend	end_to_end_tests_IE11
https://github.com/pytorch/vision	binary_linux_conda_py3.6_cu102
https://github.com/LLK/scratch-gui	unit
https://github.com/mozilla/addons-server	build-py2
https://github.com/simdjson/simdjson	clang6
https://github.com/citusdata/citus	test-12_check-mx
https://github.com/electron/electron	linux-arm-debug
https://github.com/drud/ddev	golang_test_apache_cgi
https://github.com/influxdata/influxdb	jstest
https://github.com/gatsbyjs/gatsby	e2e_tests_gatsbygram
https://github.com/cornerstonejs/cornerstoneTools	build
https://github.com/facebook/react	RELEASE_CHANNEL_stable_yarn_test
https://github.com/circleci/circleci-docs	js_build
https://github.com/ethereum/solidity	b_osx
https://github.com/storybookjs/storybook	e2e
https://github.com/plotly/plotly.js	build
https://github.com/pytorch/vision	binary_linux_wheel_py3.7_cu100
https://github.com/openlayers/openlayers	build
https://github.com/gatsbyjs/gatsby	integration_tests_long_term_caching
https://github.com/elementor/elementor	php54-wp47-build
https://github.com/mui-org/material-ui	test_browser
https://github.com/LessWrong2/Lesswrong2	build
https://github.com/artsy/reaction	auto/publish-canary
https://github.com/pytorch/vision	python_lint
https://github.com/apollographql/apollo-client	Apollo Client Monorepo
https://github.com/Harmon758/Harmonbot	test-golang
https://github.com/theQRL/QRL	integration_fuzzing
https://github.com/ant-design/ant-design	test_dom_15
https://github.com/Rise-Vision/rise-vision-apps	e2e_editor
https://github.com/ethereum/solidity	b_bytecode_ems
https://github.com/ethereum/solidity	b_ubu_clang
https://github.com/cahirwpz/mimiker	build_aarch64_gcc
https://github.com/pytorch/vision	binary_macos_conda_py2.7_cpu
https://github.com/envoyproxy/envoy	release
https://github.com/expo/expo	web_storybook
https://github.com/Rise-Vision/rise-vision-apps	dependencies
https://github.com/LessWrong2/Lesswrong2	test
https://github.com/influxdata/influxdb	jsdeps
https://github.com/hashicorp/consul	envoy-integration-test-1.13.6
https://github.com/department-of-veterans-affairs/vets-website	build-e2e
https://github.com/influxdata/influxdb	golint
https://github.com/angular/angular-cli	install
https://github.com/moul/assh	docker.build
https://github.com/wordpress-mobile/WordPress-Android	test
https://github.com/facebookresearch/ParlAI	unittests_gpu16
https://github.com/citusdata/citus	test-11_check-iso-work-fol
https://github.com/hashicorp/consul	check-vendor
https://github.com/quiltdata/quilt	test-lambdas-search
https://github.com/ory/hydra	goreleaser/test
https://github.com/electron/electron	linux-arm64-testing-gn-check
https://github.com/questdb/questdb	build
https://github.com/hashicorp/nomad	test-windows
https://github.com/facebook/buck	macos_test_android_ndk_18
https://github.com/alteryx/featuretools	py37 lint test
https://github.com/drud/ddev	golang_test_apache_fpm
https://github.com/rubocop-hq/rubocop	ruby-head-spec-with-jit
https://github.com/mapfish/mapfish-print	build
https://github.com/ethereum/solidity	t_ems_compile_ext_colony
https://github.com/cri-o/cri-o	shfmt
https://github.com/dockstore/dockstore	build
https://github.com/elementor/elementor	php-js-lints
https://github.com/cloudify-cosmo/cloudify-agent	build_agent
https://github.com/angular/angular	test_zonejs
https://github.com/tootsuite/mastodon	check-i18n
https://github.com/home-assistant/core	test 3.5.5
https://github.com/hashicorp/packer	test-windows
https://github.com/cri-o/cri-o	ginkgo
https://github.com/fluxcd/flux	helm
https://github.com/pytorch/vision	binary_linux_wheel_py2.7_cu100
https://github.com/composewell/streamly	GHCJS 8.4 + no-test + no-docs
https://github.com/neomake/neomake	coverage
https://github.com/raiden-network/raiden	test-3.7-mocked
https://github.com/robolectric/robolectric	build
https://github.com/ethereum/solidity	b_ubu_static
https://github.com/cahirwpz/mimiker	compile
https://github.com/palantir/conjure-java-runtime	trial-publish
https://github.com/dequelabs/axe-core	test_examples
https://github.com/hashicorp/nomad	test-docker
https://github.com/apollographql/apollo-client	Tests
https://github.com/rakudo/rakudo	test-macos-2
https://github.com/freedomofpress/securedrop	fetch-tor-debs
https://github.com/gatsbyjs/gatsby	integration_tests_artifacts
https://github.com/hashicorp/packer	build_windows
https://github.com/grafana/loki	build/docker-driver
https://github.com/facebook/buck	linux_test_android_ndk_18
https://github.com/hashicorp/consul	envoy-integration-test-1.13.0
https://github.com/bootstrap-vue/bootstrap-vue	setup
https://github.com/cloudify-cosmo/cloudify-manager	test_clientv3_infrastructure
https://github.com/zulip/zulip	bionic-production-install
https://github.com/compsocialscience/summer-institute	build
https://github.com/ethereum/solidity	t_ems_compile_ext_gnosis
https://github.com/angular/components	ivy_snapshot_test_cronjob
https://github.com/gatsbyjs/gatsby	theme_starters_publish
https://github.com/envoyproxy/envoy	coverage_publish
https://github.com/ethereum/solidity	t_ems_test_ext_ens
https://github.com/artsy/force	test
https://github.com/alteryx/featuretools	py38 install featuretools
https://github.com/pytorch/vision	binary_linux_conda_py3.7_cu101
https://github.com/badges/shields	services@node-14
https://github.com/facebook/react	test_devtools
https://github.com/vanilla/vanilla	chromatic_test
https://github.com/MCS-Lite/mcs-lite	test-page
https://github.com/cucumber/cucumber	messages-go
https://github.com/facebook/react	test_source
https://github.com/Azure/acs-engine	k8s-windows-1.9-release-e2e
https://github.com/cucumber/cucumber	json-formatter-ruby
https://github.com/samvera/hyrax	lint
https://github.com/citusdata/citus	test-12_check-multi
https://github.com/badges/shields	frontend
https://github.com/citusdata/citus	test-12_check-non-adaptive-isolation
https://github.com/alchemy-fr/Phraseanet	build
https://github.com/googleapis/gapic-showcase	protobufjs-load-test
https://github.com/grafana/grafana	build-all
https://github.com/facebook/react	yarn_test-www_prod_variant
https://github.com/gatsbyjs/gatsby	theme_starters_validate
https://github.com/gatsbyjs/gatsby	e2e_tests_runtime
https://github.com/googleapis/gapic-generator-typescript	showcaseLibTest
https://github.com/olistic/warriorjs	test-node-9
https://github.com/anchore/anchore-engine	lint_36
https://github.com/electron/electron	linux-checkout-fast
https://github.com/cucumber/cucumber	gherkin-go
https://github.com/api-platform/core	behat-mongodb-coverage
https://github.com/vanilla/vanilla	php_72_integration
https://github.com/ethereum/solidity	build_x86_mac
https://github.com/apollographql/apollo-client	Apollo Client
https://github.com/ethereum/solidity	test_x86_archlinux
https://github.com/demisto/content	Run Unit Testing And Lint
https://github.com/storybookjs/storybook	cli
https://github.com/elementor/elementor	php55-wp-latest-build
https://github.com/helm/charts	lint-charts
https://github.com/projectbuendia/buendia	build
https://github.com/facebook/jest	test-browser
https://github.com/Azure/acs-engine	swarm-e2e
https://github.com/cds-snc/digital-canada-ca	test
https://github.com/angular/angular	build-npm-packages
https://github.com/ethereum/solidity	t_win
https://github.com/RocketChat/Rocket.Chat.Android	build-foss-apk
https://github.com/pytorch/vision	binary_macos_conda_py3.6_cpu
https://github.com/facebook/react	test_source_fire
https://github.com/pytorch/vision	binary_linux_wheel_py2.7u_cpu
https://github.com/cucumber/cucumber	create-meta-javascript
https://github.com/facebook/react	yarn_test-classic_prod
https://github.com/facebook/buck	linux_test_android_ndk_16
https://github.com/uktrade/data-hub-frontend	lep_staff_at
https://github.com/userdive/agent.js	test
https://github.com/grpc-ecosystem/grpc-gateway	test
https://github.com/raiden-network/raiden	smoketest-udp-3.6
https://github.com/envoyproxy/envoy	compile_time_options
https://github.com/cBioPortal/cbioportal-frontend	install
https://github.com/ethereum/solidity	b_ems
https://github.com/elementor/elementor	php54-wp49-build
https://github.com/ant-design/ant-design	lint
https://github.com/tendermint/tendermint	test_abci_cli
https://github.com/algolia/algoliasearch-helper-js	build
https://github.com/rubocop-hq/rubocop	ruby-head-rubocop-with-jit
https://github.com/algolia/instantsearch.js	test_unit
https://github.com/palantir/conjure-java-runtime	unit-test
https://github.com/artsy/reaction	type-check
https://github.com/electron/electron	linux-arm-testing
https://github.com/apollographql/apollo-server	NodeJS 12
https://github.com/forcedotcom/salesforcedx-vscode	unit-tests
https://github.com/massgov/mayflower	build
https://github.com/raiden-network/raiden	test-unit-3.6
https://github.com/facebook/react	build
https://github.com/PyTorchLightning/pytorch-lightning	PyTorch-v1.1
https://github.com/ory/hydra	test
https://github.com/angular/angular	test_saucelabs_bazel
https://github.com/Azure/acs-engine	k8s-linux-default-e2e
https://github.com/Automattic/simplenote-electron	artifacts
https://github.com/tootsuite/mastodon	install-ruby2.6
https://github.com/artsy/force	jest-v2
https://github.com/nusmodifications/nusmods	nus-scrapers-v2
https://github.com/bazelbuild/rules_nodejs	setup
https://github.com/expo/expo	android_test_suite
https://github.com/electron/electron	linux-x64-testing-verify-ffmpeg
https://github.com/LLK/scratch-gui	store_dist
https://github.com/wordpress-mobile/WordPress-iOS	UI Tests (iPhone 11)
https://github.com/simdjson/simdjson	gcc-avx-dynamic
https://github.com/tendermint/tendermint	test_p2p_ipv6
https://github.com/aeternity/aeternity	test_lima_otp21
https://github.com/angular/angular-cli	e2e-cli-win";
            var sr = new StringReader(hardcode);
            var lines = new List<(string, string)>();
            while (sr.ReadLine() is string l)
            {
                var array = l.Split('\t');
                if (array.Length != 2)
                {
                    throw new FormatException($"Line {l} is not in correct format");
                }

                lines.Add((array[0], array[1]));
            }
            jobs = lines.ToArray();
        }

        _logger.LogInformation("Adding {JobCount} jobs.", jobs.Length);
        _forecastingContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var savedSamples = await _forecastingContext.AccelerationSamples.Select(s => new { s.VcsUrl, s.JobName }).ToListAsync().ConfigureAwait(false);
        var toAdd = jobs.Except(savedSamples.Select(s => (s.VcsUrl, s.JobName))).ToList();

        _logger.LogInformation("Previously sampled {Saved} jobs. {ToAddCount} jobs to add.", savedSamples.Count, toAdd.Count);
        for (int i = 0; i < toAdd.Count; i++)
        {
            var (vcsUrl, jobName) = toAdd[i];
            var count = await _forecastingContext.Builds.CountAsync(b => b.VcsUrl == vcsUrl && b.JobName == jobName).ConfigureAwait(false);
            _forecastingContext.AccelerationSamples.Add(new AccelerationSample
            {
                VcsUrl = vcsUrl,
                JobName = jobName,
                BuildCount = count,
            });
        }
        await _forecastingContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask CreateRandom()
    {
        var jobs = await _forecastingContext.JobInfos.Where(j => !j.RandomValueCreated).ToListAsync().ConfigureAwait(false);
        _logger.LogInformation($"Creating random values for {jobs.Count} jobs");
        int i = 1;
        var random = new Random();
        var ulongBytes = new byte[sizeof(ulong)];
        foreach (var job in jobs)
        {
            _logger.LogInformation($"{i++}/{jobs.Count} {job.VcsUrl}/{job.JobName}");
            random.NextBytes(ulongBytes);
            job.Random = BitConverter.ToUInt64(ulongBytes);
            job.RandomValueCreated = true;
        }
        _logger.LogInformation($"Saving {jobs.Count} jobs");
        await _forecastingContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = random.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
