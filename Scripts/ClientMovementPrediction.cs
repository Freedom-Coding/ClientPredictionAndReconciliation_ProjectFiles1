using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ClientMovementPrediction : NetworkBehaviour
{
    //Both Client And Server Specific
    [SerializeField] float movementSpeedBase = 5;
    [SerializeField] private int tickRate = 60;
    [SerializeField] int currentTick;
    private float time;
    private float tickTime;

    //Client Specific
    private const int BUFFERSIZE = 1024;
    [SerializeField] MovementData[] clientMovementDatas = new MovementData[BUFFERSIZE];
    private Animator animator;
    private Rigidbody2D rb;

    //Server Specific
    [SerializeField] private float maxPositionError = 0.5f;


    private void Awake()
    {
        tickTime = 1f / tickRate;
    }


    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        time += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!IsClient || !IsOwner) return;

        while (time > tickTime)
        {
            currentTick++;
            time -= tickTime;

            Move();
        }
    }

    private void Move()
    {
        Vector2 movementDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector2 moveVector = movementDirection.normalized * movementSpeedBase;

        animator.SetFloat("Speed", moveVector.magnitude);
        rb.velocity = moveVector;

        if (moveVector != Vector2.zero)
        {
            animator.SetFloat("Horizontal", moveVector.normalized.x);
            animator.SetFloat("Vertical", moveVector.normalized.y);
        }

        clientMovementDatas[currentTick % BUFFERSIZE] = new MovementData
        {
            tick = currentTick,
            movementDirection = movementDirection,
            position = transform.position
        };


        if (currentTick < 2) return;

        MoveServerRPC(clientMovementDatas[currentTick % BUFFERSIZE], clientMovementDatas[(currentTick - 1) % BUFFERSIZE],
                new ServerRpcParams { Receive = new ServerRpcReceiveParams { SenderClientId = OwnerClientId } });

    }

    [ServerRpc]
    private void MoveServerRPC(MovementData currentMovementData, MovementData lastMovementData, ServerRpcParams parameters)
    {
        Vector2 startPosition = transform.position;

        Vector2 moveVector = lastMovementData.movementDirection.normalized * movementSpeedBase;
        Physics.simulationMode = SimulationMode.Script;
        transform.position = lastMovementData.position;
        rb.velocity = moveVector;
        Physics.Simulate(Time.fixedDeltaTime);
        Vector2 correctPosition = transform.position;
        transform.position = startPosition;
        Physics.simulationMode = SimulationMode.FixedUpdate;

        if (Vector2.Distance(correctPosition, currentMovementData.position) > maxPositionError)
        {
            Debug.Log("Position is off");

            ReconciliateClientRPC(currentMovementData.tick, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new List<ulong>() { parameters.Receive.SenderClientId }
                }
            });

        }
    }

    [ClientRpc]
    private void ReconciliateClientRPC(int activationTick, ClientRpcParams parameters)
    {
        Vector2 correctPosition = clientMovementDatas[(activationTick - 1) % BUFFERSIZE].position;

        Physics.simulationMode = SimulationMode.Script;
        while (activationTick <= currentTick)
        {
            Vector2 moveVector = clientMovementDatas[(activationTick - 1) % BUFFERSIZE].movementDirection.normalized * movementSpeedBase;
            transform.position = correctPosition;
            rb.velocity = moveVector;
            Physics.Simulate(Time.fixedDeltaTime);
            correctPosition = transform.position;
            clientMovementDatas[activationTick % BUFFERSIZE].position = correctPosition;
            activationTick++;
        }
        Physics.simulationMode = SimulationMode.FixedUpdate;

        transform.position = correctPosition;
    }


    [System.Serializable]
    public class MovementData : INetworkSerializable
    {
        public int tick;
        public Vector2 movementDirection;
        public Vector2 position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref movementDirection);
            serializer.SerializeValue(ref position);
        }
    }
}