using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MessagePack;
using MessagePack.Resolvers;
using REstate.Remote.Models;
using REstate.Remote.Services;
using REstate.Schematics;

namespace REstate.Remote
{
    public class GrpcStateEngine<TState, TInput>
        : IRemoteStateEngine<TState, TInput>
    {
        private readonly IStateMachineService _stateMachineService;

        public GrpcStateEngine(IStateMachineService stateMachineService)
        {
            _stateMachineService = stateMachineService
                .WithHeaders(new Metadata
                {
                    { "State-Type", typeof(TState).AssemblyQualifiedName },
                    { "Input-Type", typeof(TInput).AssemblyQualifiedName }
                });
        }

        public Task<IStateMachine<TState, TInput>> CreateMachineAsync(
            ISchematic<TState, TInput> schematic,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default(CancellationToken))
            => CreateMachineAsync(schematic.Copy(), metadata, cancellationToken);

        public Task<IStateMachine<TState, TInput>> CreateMachineAsync(
            Schematic<TState, TInput> schematic,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return null;
        }

        public Task<IStateMachine<TState, TInput>> CreateMachineAsync(
            string schematicName,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return null;
        }

        public async Task DeleteMachineAsync(
            string machineId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _stateMachineService.DeleteMachineAsync(new DeleteMachineRequest
            {
                MachineId = machineId
            });
        }

        public Task<IStateMachine<TState, TInput>> GetMachineAsync(
            string machineId,
            CancellationToken cancellationToken = default(CancellationToken)) =>
                Task.FromResult<IStateMachine<TState, TInput>>(
                    new GrpcStateMachine<TState, TInput>(_stateMachineService, machineId));


        public async Task<ISchematic<TState, TInput>> GetSchematicAsync(
            string schematicName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _stateMachineService.GetSchematicAsync(new GetSchematicRequest
            {
                SchematicName = schematicName
            });

            return MessagePackSerializer.Deserialize<Schematic<TState, TInput>>(
                response.SchematicBytes,
                ContractlessStandardResolver.Instance);
        }

        public async Task<ISchematic<TState, TInput>> StoreSchematicAsync(
            Schematic<TState, TInput> schematic,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _stateMachineService.StoreSchematicAsync(new StoreSchematicRequest
            {
                SchematicBytes = MessagePackSerializer.Serialize(
                    schematic,
                    ContractlessStandardResolver.Instance)
            });

            return MessagePackSerializer.Deserialize<Schematic<TState, TInput>>(
                response.SchematicBytes,
                ContractlessStandardResolver.Instance);
        }

        public Task<ISchematic<TState, TInput>> StoreSchematicAsync(
            ISchematic<TState, TInput> schematic,
            CancellationToken cancellationToken = default(CancellationToken)) 
            => StoreSchematicAsync(schematic.Copy(), cancellationToken);
    }
}