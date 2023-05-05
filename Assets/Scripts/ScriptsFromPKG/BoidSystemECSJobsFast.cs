/** 
* Parts of this code were originally made by Bogdan Codreanu
* Original code: https://github.com/BogdanCodreanu/ECS-Boids-Murmuration_Unity_2019.1
*/ 

using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class BoidSystemECSJobsFast : JobComponentSystem {

    private EntityQuery boidGroup;

    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////// NEW 2 ////////////////////////////////////////////////////////////////////////
    /// </summary>
    /*private EntityQuery BoidTargetsGroup;*/


    /// ///////////////////////////////////////////////////////////////////////////////////////////// TO HERE ////////////////////////////////////////////////////////////////////////

    private BoidControllerECSJobsFast controller;

    // Copies all boid positions and headings into buffer
    [BurstCompile]
    [RequireComponentTag(typeof(BoidECSJobsFast))]
    private struct CopyPositionsAndHeadingsInBuffer : IJobForEachWithEntity<LocalToWorld> {
        public NativeArray<float3> boidPositions;
        public NativeArray<float3> boidHeadings;

        public void Execute(Entity boid, int boidIndex, [ReadOnly] ref LocalToWorld localToWorld) {
            boidPositions[boidIndex] = localToWorld.Position;
            boidHeadings[boidIndex] = localToWorld.Forward;
        }
    }

    /////////////NEWW 2
    /*private struct CellsData
    {
        public NativeArray<int> closestTargetIndices;
    }*/
    /////////////TO HERE

    // Asigns each boid to a cell. Each boid index is stored in the hashMap. Each hash corresponds to a cell.
    // The cell grid has a random offset and rotation each frame to remove artefacts.
    [BurstCompile]
    [RequireComponentTag(typeof(BoidECSJobsFast))]
    private struct HashPositionsToHashMap : IJobForEachWithEntity<LocalToWorld> {
        public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
        [ReadOnly] public quaternion cellRotationVary;
        [ReadOnly] public float3 positionOffsetVary;
        [ReadOnly] public float cellRadius;

        public void Execute(Entity boid, int boidIndex, [ReadOnly] ref LocalToWorld localToWorld) {
            var hash = (int)math.hash(new int3(math.floor(math.mul(cellRotationVary, localToWorld.Position + positionOffsetVary) / cellRadius)));
            hashMap.Add(hash, boidIndex);
        }
    }

    // Sums up positions and headings of all boids of each cell. These sums are stored in the
    // same array as before (cellPositions and cellHeadings), so that there is no need for
    // a new array. The index of each cell is set to the index of the first boid.
    // With the array indicesOfCells each boid can find the index of its cell.
    // This way every boid knows the sum of all the positions (and headings) of all the other
    // boids in the same cell -> no nested loop required -> massive performance boost
    [BurstCompile]
    private struct MergeCellsJob : IJobNativeMultiHashMapMergedSharedKeyIndices {
        public NativeArray<int> indicesOfCells;
        public NativeArray<float3> cellPositions;
        public NativeArray<float3> cellHeadings;
        public NativeArray<int> cellCount;

        /// <summary>
       /* ////////////////////////////////////////////////////////////////////// NEW (2nd) ///////////////////////////////////////////////////////////////////////////////////////////
        
        [ReadOnly] public NativeArray<float3> targetsPositions;
        public NativeArray<int> closestTargetIndexToCells;

        

        public struct IntFloat
        {

            public IntFloat(int i1, float f1)
            {
                i = i1;
                f = f1;
            }

            public int i;
            public float f;
        }

        public IntFloat CalculateIndexOfClosestPosition(NativeArray<float3> searchedPositions, float3 position)
        {
            int nearestPositionIndex = 0;
            if (searchedPositions.Length == 0)
            {
                return new IntFloat(-1, 0);
            }
            float nearestDistanceSq = math.lengthsq(position - searchedPositions[0]);

            for (int i = 0; i < searchedPositions.Length; i++)
            {
                float3 targetPosition = searchedPositions[i];
                float distanceToThisPos = math.lengthsq(position - searchedPositions[i]);
                bool isThisNearer = distanceToThisPos < nearestDistanceSq;

                nearestDistanceSq = math.select(nearestDistanceSq, distanceToThisPos, isThisNearer);
                nearestPositionIndex = math.select(nearestPositionIndex, i, isThisNearer);
            }

            return new IntFloat(nearestPositionIndex, nearestDistanceSq);
        }

        // index is the first value encountered at a hash.
        // the key hash cannot be accessed.
        // this value is the entity index.
        public void ExecuteFirst(int firstBoidIndexEncountered)
        {
            indicesOfCells[firstBoidIndexEncountered] = firstBoidIndexEncountered;
            
            float3 positionInThisCell = cellPositions[firstBoidIndexEncountered] / cellCount[firstBoidIndexEncountered];

            // calculate index of the closest target
            var targetsResult = CalculateIndexOfClosestPosition(targetsPositions, positionInThisCell);
            closestTargetIndexToCells[firstBoidIndexEncountered] = targetsResult.i;


            /// </summary> //////////////////////////////////////////////////////////////// HASTA AQUI //////////////////////////////////////////////////////////////////////////////////////////
            /// 
            
        }*/

        public void ExecuteFirst(int firstBoidIndexEncountered)
        {
            indicesOfCells[firstBoidIndexEncountered] = firstBoidIndexEncountered;
            cellCount[firstBoidIndexEncountered] = 1;
            float3 positionInThisCell = cellPositions[firstBoidIndexEncountered] / cellCount[firstBoidIndexEncountered];
        }

            public void ExecuteNext(int firstBoidIndexAsCellKey, int boidIndexEncountered) {
            cellCount[firstBoidIndexAsCellKey] += 1;
            cellHeadings[firstBoidIndexAsCellKey] += cellHeadings[boidIndexEncountered];
            cellPositions[firstBoidIndexAsCellKey] += cellPositions[boidIndexEncountered];
            indicesOfCells[boidIndexEncountered] = firstBoidIndexAsCellKey;
        }
    }

    // Calculates the forces for each boid (no nested loop). All forces are weighted, added up
    // and directly applied to orientation and position.
    [BurstCompile]
    [RequireComponentTag(typeof(BoidECSJobsFast))]
    private struct MoveBoids : IJobForEachWithEntity<LocalToWorld> {
        
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float boidSpeed;

        [ReadOnly] public float separationWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float cohesionWeight;


        /// <summary>
        /// NEW////////////////////////////////////////////////////////////////////
        /// </summary>
        [ReadOnly] public float3 targetPos;
        [ReadOnly] public float targetWeight;
        /// <summary>
        /// ////////////////////////////////////////////////////////////////////
        /// </summary>
        /// 
        /// <summary>
        /*/// //////////////////////////////// NEW 2////////////////////////////////////////////////////////////////////
        /// </summary>
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> targetsPositions;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellClosestTargetsIndices;
        /// <summary>*/

        [ReadOnly] public float cageSize;
        [ReadOnly] public float cageAvoidDist;
        [ReadOnly] public float cageAvoidWeight;

        [ReadOnly] public float cellSize;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellIndices;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> positionSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> headingSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellBoidCount;

        public void Execute(Entity boid, int boidIndex, ref LocalToWorld localToWorld) {

            float3 boidPosition = localToWorld.Position;
            int cellIndex = cellIndices[boidIndex];

            int nearbyBoidCount = cellBoidCount[cellIndex] - 1;
            float3 positionSum = positionSumsOfCells[cellIndex] - localToWorld.Position;
            float3 headingSum = headingSumsOfCells[cellIndex] - localToWorld.Forward;

            float3 force = float3.zero;

            if (nearbyBoidCount > 0) {
                float3 averagePosition = positionSum / nearbyBoidCount;

                float distToAveragePositionSq = math.lengthsq(averagePosition - boidPosition);
                float maxDistToAveragePositionSq = cellSize * cellSize;

                float distanceNormalized = distToAveragePositionSq / maxDistToAveragePositionSq;
                float needToLeave = math.max(1 - distanceNormalized, 0f);

                float3 toAveragePosition = math.normalizesafe(averagePosition - boidPosition);
                float3 averageHeading = headingSum / nearbyBoidCount;


                force += -toAveragePosition * separationWeight * needToLeave;
                force +=  toAveragePosition * cohesionWeight;
                force +=  averageHeading    * alignmentWeight;

                //<<<<<<<<<<<<<<<<--------/////////////////////// NEW ////////////////////////////--------->>>>>>>>>>>>>>

                float3 toTargetPosition = math.normalizesafe(averagePosition - targetPos); ;
                force += -math.normalize(toTargetPosition) * targetWeight;

                //<<<<<<<<<<<<<<<<--------/////////////////////// NEW 2 ////////////////////////////--------->>>>>>>>>>>>>>
                /*
                int indexOfClosestTarget = cellClosestTargetsIndices[cellIndex];
                if (indexOfClosestTarget >= 0)
                {
                    float3 toTargetsPosition = math.normalizesafe(averagePosition - targetsPositions[indexOfClosestTarget]);
                    force += -math.normalize(toTargetsPosition) * targetWeight;
                }
                */
                //<<<<<<<<<<<<<<<<--------/////////////////////// TO HERE ////////////////////////////--------->>>>>>>>>>>>>>
            }

            if (math.min(math.min(
                (cageSize / 2f) - math.abs(boidPosition.x),
                (cageSize / 2f) - math.abs(boidPosition.y)),
                (cageSize / 2f) - math.abs(boidPosition.z))
                    < cageAvoidDist) {
                force += -math.normalize(boidPosition) * cageAvoidWeight;
            }

           

            float3 velocity = localToWorld.Forward * boidSpeed;
            velocity += force * deltaTime;
            velocity = math.normalize(velocity) * boidSpeed;

            localToWorld.Value = float4x4.TRS(
                localToWorld.Position + velocity * deltaTime,
                quaternion.LookRotationSafe(velocity, localToWorld.Up),
                new float3(1f)
            );
        }
    }

    protected override void OnCreate() {
        boidGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<BoidECSJobsFast>(), ComponentType.ReadWrite<LocalToWorld>() },
            Options = EntityQueryOptions.FilterWriteGroup
        });
        ////////////////////////////////////////////////////////////// NEW 2 ///////////////////////////////////////////////////////////////////////
        ///BoidTargetsGroup = GetEntityQuery(ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<BoidTarget>());
        ///////////////////////////////////////////////////////////// HERE /////////////////////////////////////////////////////////////////////////////
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {

        

        if (!controller) {
            controller = BoidControllerECSJobsFast.Instance;
        }
        if (controller) {
            int boidCount = boidGroup.CalculateEntityCount();
            ////////////////////////////////////////////////////////// NEW 2 //////////////////////////////////////////////////////////////////////
            //int targetsCount = BoidTargetsGroup.CalculateEntityCount();
            /////////////////////////////////////////////////////////// HERE //////////////////////////////////////////////////////////////////////
            ///

            var cellIndices = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellBoidCount = new NativeArray<int>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var boidPositions = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var boidHeadings = new NativeArray<float3>(boidCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hashMap = new NativeMultiHashMap<int, int>(boidCount, Allocator.TempJob);

            /////////////////////////////////////////////////////////// NEW 2 //////////////////////////////////////////////////////////////////////
            /*var targetsPositions = new NativeArray<float3>(targetsCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var closestTargetIndices = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);*/
            /////////////////////////////////////////////////////////// HERE //////////////////////////////////////////////////////////////////////
            ///

            var positionsAndHeadingsCopyJob = new CopyPositionsAndHeadingsInBuffer {
                boidPositions = boidPositions,
                boidHeadings = boidHeadings
            };
            JobHandle positionsAndHeadingsCopyJobHandle = positionsAndHeadingsCopyJob.Schedule(boidGroup, inputDeps);

            quaternion randomHashRotation = quaternion.Euler(
                UnityEngine.Random.Range(-360f, 360f),
                UnityEngine.Random.Range(-360f, 360f),
                UnityEngine.Random.Range(-360f, 360f)
            );
            float offsetRange = controller.boidPerceptionRadius / 2f;
            float3 randomHashOffset = new float3(
                UnityEngine.Random.Range(-offsetRange, offsetRange),
                UnityEngine.Random.Range(-offsetRange, offsetRange),
                UnityEngine.Random.Range(-offsetRange, offsetRange)
            );

            var hashPositionsJob = new HashPositionsToHashMap {
                hashMap = hashMap.AsParallelWriter(),
                cellRotationVary = randomHashRotation,
                positionOffsetVary = randomHashOffset,
                cellRadius = controller.boidPerceptionRadius,
            };
            JobHandle hashPositionsJobHandle = hashPositionsJob.Schedule(boidGroup, inputDeps);
            
            // Proceed when these two jobs have been completed
            JobHandle copyAndHashJobHandle = JobHandle.CombineDependencies(
                positionsAndHeadingsCopyJobHandle,
                hashPositionsJobHandle
            );
            ///////////////////////////////////////////////////// NEW 3 /////////////////////////////////////////////////////////////////////////////////////
            /*var newCellData = new CellsData
            {
                closestTargetIndices = closestTargetIndices,
            };*/
            ///////////////////////////////////////////////////// TO HERE /////////////////////////////////////////////////////////////////////////////////////

            var mergeCellsJob = new MergeCellsJob {
                indicesOfCells = cellIndices,
                cellPositions = boidPositions,
                cellHeadings = boidHeadings,
                cellCount = cellBoidCount,

                ///////////////////////////////////////////////////// NEW 2 /////////////////////////////////////////////////////////////////////////////////////
                /*targetsPositions = targetsPositions,
                closestTargetIndexToCells = closestTargetIndices,*/
                ///////////////////////////////////////////////////// TO HERE /////////////////////////////////////////////////////////////////////////////////////
            };
            JobHandle mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, copyAndHashJobHandle);

            var moveJob = new MoveBoids {
                deltaTime = Time.DeltaTime,
                boidSpeed = controller.boidSpeed,

                separationWeight = controller.separationWeight,
                alignmentWeight = controller.alignmentWeight,
                cohesionWeight = controller.cohesionWeight,

                //<<<<<<<<<<<<<<<<--------/////////////////////// NEW ////////////////////////////--------->>>>>>>>>>>>>>
                targetWeight = controller.targetWeight,
                targetPos = controller.targetPosition,

                ///////////////////////////////////////////////////// NEW 2 /////////////////////////////////////////////////////////////////////////////////////
                /*targetsPositions = targetsPositions,
                cellClosestTargetsIndices = closestTargetIndices,*/
                //<<<<<<<<<<<<<<<<--------/////////////////////// TO HERE ////////////////////////////--------->>>>>>>>>>>>>>

                cageSize = controller.cageSize,
                cageAvoidDist = controller.avoidWallsTurnDist,
                cageAvoidWeight = controller.avoidWallsWeight,

                cellSize = controller.boidPerceptionRadius,
                cellIndices = cellIndices,
                positionSumsOfCells = boidPositions,
                headingSumsOfCells = boidHeadings,
                cellBoidCount = cellBoidCount,
                
            };
            JobHandle moveJobHandle = moveJob.Schedule(boidGroup, mergeCellsJobHandle);
            moveJobHandle.Complete();
            hashMap.Dispose();

            inputDeps = moveJobHandle;
            boidGroup.AddDependency(inputDeps);
            
            return inputDeps;
        }
        else {
            return inputDeps;
        }
    }
    

}
