using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class DPoSSideChainTests : DPoSSideChainTestBase
    {
        private DPoSSideChainTester TesterManager { get; set; }

        public DPoSSideChainTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }

        [Fact]
        public async Task Validation_ConsensusAfterExecution_Success()
        {
            TesterManager.InitialTesters();

            var dposInformation = new DPoSHeaderInformation();

            var validationResult = await TesterManager.Testers[0].ValidateConsensusAfterExecutionAsync(dposInformation);
            validationResult.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task Get_ConsensusCommand_Success()
        {
            TesterManager.InitialTesters();

            var consensusCommand = await TesterManager.Testers[0].GetConsensusCommandAsync();
            consensusCommand.ShouldNotBeNull();

            var behavior = DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour;
            behavior.ShouldBe(DPoSBehaviour.UpdateValue);
        }

        [Fact]
        public async Task InitialConsensus_Failed()
        {
            TesterManager.InitialTesters();

            //invalid round number
            {
                var roundInformation = new Round
                {
                    RoundNumber = 2
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.InitialConsensus), roundInformation);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid round number").ShouldBeTrue();
            }

            //invalid miners info
            {
                var roundInformation = new Round
                {
                    RoundNumber = 1,
                    RealTimeMinersInformation = { }
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.InitialConsensus), roundInformation);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("No miner in input data").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task InitialConsensus_Success()
        {
            TesterManager.InitialTesters();

            var roundInformation = new Round
            {
                RoundNumber = 1,
                RealTimeMinersInformation =
                {
                    {
                        TesterManager.Testers[0].PublicKey, new MinerInRound
                        {
                            Alias = "first-bp",
                            Order = 1,
                            ExpectedMiningTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)).ToTimestamp(),
                        }
                    }
                }
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.InitialConsensus), roundInformation);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var currentRound = await TesterManager.Testers[0].GetCurrentRoundInformation();
            currentRound.RoundNumber.ShouldBe(1);
        }

        [Fact]
        public async Task Set_ConfigStrategy()
        {
            TesterManager.InitialTesters();

            var input = new DPoSStrategyInput
            {
                IsBlockchainAgeSettable = true,
                IsTimeSlotSkippable = true,
                IsVerbose = true
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //set again
            transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already configured").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateValue_Failed()
        {
            TesterManager.InitialTesters();

            //invalid round number
            var input = new ToUpdate
            {
                RoundId = 1234
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.UpdateValue), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Round information not found").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateValue_Success()
        {
            TesterManager.InitialTesters();

            await InitialConsensus_Success();
            var roundInfo = await TesterManager.Testers[0].GetCurrentRoundInformation();

            var input = new ToUpdate
            {
                RoundId = roundInfo.RoundId,
                Signature = Hash.Generate(),
                OutValue = Hash.Generate(),
                ProducedBlocks = 1,
                ActualMiningTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4)).ToTimestamp(),
                SupposedOrderOfNextRound = 1,
                PromiseTinyBlocks = 2,
                PreviousInValue = Hash.Generate(),
                MinersPreviousInValues =
                {
                    {TesterManager.Testers[0].PublicKey, Hash.Generate()}
                }
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.UpdateValue), input);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task NextRound_Failed()
        {
            TesterManager.InitialTesters();
            await InitialConsensus_Success();
            
            var input = new Round
            {
                RoundNumber = 1,
                BlockchainAge = 10
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Incorrect round number for next round.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task NextRound_Success()
        {
            TesterManager.InitialTesters();
            await InitialConsensus_Success();
            
            var input = new Round
            {
                RoundNumber = 2,
                BlockchainAge = 10,
                ExtraBlockProducerOfPreviousRound = TesterManager.Testers[1].PublicKey
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}