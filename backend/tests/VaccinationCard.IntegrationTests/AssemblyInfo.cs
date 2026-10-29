using VaccinationCard.IntegrationTests;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(IntegrationFixture))]
[assembly: Parallelization(Mode = ParallelMode.None)]
