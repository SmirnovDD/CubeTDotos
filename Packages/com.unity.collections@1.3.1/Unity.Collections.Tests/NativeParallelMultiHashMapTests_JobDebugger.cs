using NUnit.Framework;
using System;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.Tests;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
internal class NativeParallelMultiHashMapTests_JobDebugger : NativeParallelMultiHashMapTestsFixture
{
    [Test]
    public void NativeParallelMultiHashMap_Read_And_Write_Without_Fences()
    {
        var hashMap = new NativeParallelMultiHashMap<int, int>(hashMapSize, CommonRwdAllocator.Handle);
        var writeStatus = CollectionHelper.CreateNativeArray<int>(hashMapSize, CommonRwdAllocator.Handle);
        var readValues = CollectionHelper.CreateNativeArray<int>(hashMapSize, CommonRwdAllocator.Handle);

        var writeData = new MultiHashMapWriteParallelForJob()
        {
            hashMap = hashMap.AsParallelWriter(),
            status = writeStatus,
            keyMod = hashMapSize,
        };

        var readData = new MultiHashMapReadParallelForJob()
        {
            hashMap = hashMap,
            values = readValues,
            keyMod = writeData.keyMod,
        };

        var writeJob = writeData.Schedule(hashMapSize, 1);
        Assert.Throws<InvalidOperationException>(() => { readData.Schedule(hashMapSize, 1); });
        writeJob.Complete();

        hashMap.Dispose();
        writeStatus.Dispose();
        readValues.Dispose();
    }
}
#endif
