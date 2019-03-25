using AElf.Sdk.CSharp;
using Google.Protobuf;

namespace AElf.Runtime.CSharp.Tests.TestContract
{
    public class ContractApi : TestContractContainer.TestContractBase
    {
        public override BoolOutput TestBoolState(BoolInput input)
        {
            State.BoolInfo.Value = input.BoolValue;
            return new BoolOutput()
            {
                BoolValue = State.BoolInfo.Value
            };
        }

        public override Int32Output TestInt32State(Int32Input input)
        {
            State.Int32Info.Value = State.Int32Info.Value.Sub(input.Int32Value);
            return new Int32Output()
            {
                Int32Value = State.Int32Info.Value
            };
        }

        public override UInt32Output TestUInt32State(UInt32Input input)
        {
            State.UInt32Info.Value = State.UInt32Info.Value.Add(input.UInt32Value);
            return new UInt32Output()
            {
                UInt32Value = State.UInt32Info.Value
            };
        }

        public override Int64Output TestInt64State(Int64Input input)
        {
            State.Int64Info.Value = State.Int64Info.Value.Sub(input.Int64Value);
            return new Int64Output()
            {
                Int64Value = State.Int64Info.Value
            };
        }

        public override UInt64Output TestUInt64State(UInt64Input input)
        {
            State.UInt64Info.Value = State.UInt64Info.Value.Sub(input.UInt64Value);
            return new UInt64Output()
            {
                UInt64Value = State.UInt64Info.Value
            };
        }

        public override StringOutput TestStringState(StringInput input)
        {
            State.StringInfo.Value = State.StringInfo.Value + input.StringValue;
            return new StringOutput()
            {
                StringValue = State.StringInfo.Value
            };
        }

        public override BytesOutput TestBytesState(BytesInput input)
        {
            State.BytesInfo.Value = input.BytesValue.ToByteArray();
            return new BytesOutput()
            {
                BytesValue = ByteString.CopyFrom(State.BytesInfo.Value)
            };
        }

        public override ProtobufOutput TestProtobufState(ProtobufInput input)
        {
            var boolValue = State.ProtoInfo.Value.BoolValue;
            if (boolValue)
            {
                State.ProtoInfo.Value = input.ProtobufValue;
            }
            
            return new ProtobufOutput()
            {
                ProtobufValue = State.ProtoInfo.Value
            };
        }

        public override Complex1Output TestComplex1State(Complex1Input input)
        {
            State.BoolInfo.Value = input.BoolValue;
            State.Int32Info.Value = input.Int32Value;
            
            return new Complex1Output()
            {
                BoolValue = State.BoolInfo.Value,
                Int32Value = State.Int32Info.Value
            };
        }

        public override Complex2Output TestComplex2State(Complex2Input input)
        {
            State.BoolInfo.Value = input.BoolData.BoolValue;
            State.Int32Info.Value = input.Int32Data.Int32Value;
            
            return new Complex2Output()
            {
                BoolData = new BoolOutput(){ BoolValue = State.BoolInfo.Value },
                Int32Data = new Int32Output() { Int32Value = State.Int32Info.Value }
            };
        }
    }
}