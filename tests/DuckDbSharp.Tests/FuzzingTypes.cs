namespace DuckDbSharp.FuzzingTypes
{


    public class MyClass3
    {
        //public InnerObj2? InnerObj;
        public EvenInnerStruct?[] TheArray;
    }

    public record MyClass4(SomeStruct? SomeOptionalStruct);

    public record MyClass(
        int Id,
        WithNullishDefault WithNullishDefault,
        SomeStruct SomeStruct,
        SomeStruct? SomeOptionalStruct,
        InnerObj? InnerObj,
        string? C,
        int[] ListOfInt,
        int?[] ListOfOptInt,
        byte[] Bytes,
        EvenInner?[] ListOfObjs,
        EvenInnerStruct[] ListOfNonNullStructs,
        EvenInnerStruct?[] ListOfOptionalStructs,
        WithNullishDefault[] ListWithNullishDefault
        )
    {
        //public EvenInnerStruct?[] ListOfNullableStructAsField;
    }
    [DuckDbDefaultValueIsNullish]
    public record struct WithNullishDefault(long Num, string? Str);

    public record struct SomeStruct(long Num);
    public class InnerObj2
    {
        public int Val2;
    }
    public record InnerObj(
        int? TheVal,
        int Val2,
        string? B,
        EvenInner? EvenInner,
        int[] ListOfIntInner,
        int?[] ListOfOptIntInner,
        EvenInner?[] ListOfObjs,
        EvenInnerStruct[] ListOfNonNullStructs,
        EvenInnerStruct?[] ListOfOptionalStructs
        );
    public record EvenInner(int? Q);
    [DuckDbDefaultValueIsNullish]
    public struct EvenInner2
    {
        public int Q;
    }
    public record struct EvenInnerStruct(int TheVal);
    public record struct MyClass2(EvenInner2[] ListOfStructs);
}

