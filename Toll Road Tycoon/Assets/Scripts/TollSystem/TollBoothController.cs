using System;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.TollSystem
{
    /// <summary>Runs the booth state machine: wait, take payment, open barrier, let the car pass, close.</summary>
    public sealed class TollBoothController
    {
        private const double StopTolerance = 0.06;
        private const double StoppedSpeed = 0.25;

        private readonly PaymentProcessor _payments;
        private readonly LaneManager _lanes;
        private readonly Core.GameStats _stats;

        public event Action<TollBooth> BarrierOpened;
        public event Action<TollBooth> BoothClosed;

        public TollBoothController(PaymentProcessor payments, LaneManager lanes, Core.GameStats stats)
        {
            _payments = payments;
            _lanes = lanes;
            _stats = stats;
        }

        public void Tick(TollBooth booth, double now, double dt)
        {
            if (booth.State != BoothState.Closed && booth.State != BoothState.Idle && booth.State != BoothState.WaitingForVehicle)
            {
                booth.BusySeconds += dt;
            }

            switch (booth.State)
            {
                case BoothState.Closed:
                    if (booth.IsAcceptingVehicles) booth.State = BoothState.Idle;
                    break;

                case BoothState.Idle:
                case BoothState.WaitingForVehicle:
                    TickWaiting(booth, now);
                    break;

                case BoothState.ProcessingPayment:
                    booth.Timer -= dt;
                    if (booth.Timer <= 0)
                    {
                        _payments.TryProcess(booth.CurrentVehicle, booth, out _);
                        booth.CurrentVehicle.State = VehicleState.WaitingForBarrier;
                        booth.State = BoothState.OpeningBarrier;
                        booth.Timer = booth.BarrierSeconds;
                    }
                    break;

                case BoothState.OpeningBarrier:
                    booth.Timer -= dt;
                    booth.BarrierOpen = Clamp01(1 - booth.Timer / booth.BarrierSeconds);
                    if (booth.Timer <= 0)
                    {
                        booth.BarrierOpen = 1;
                        booth.CurrentVehicle.BarrierCleared = true;
                        booth.CurrentVehicle.State = VehicleState.PassingBarrier;
                        booth.State = BoothState.VehiclePassing;
                        BarrierOpened?.Invoke(booth);
                    }
                    break;

                case BoothState.VehiclePassing:
                    var v = booth.CurrentVehicle;
                    if (v.Segment != booth.Lane || v.Rear > _lanes.BarrierDistance + 0.1)
                    {
                        booth.AssignedVehicles.Remove(v);
                        booth.CurrentVehicle = null;
                        booth.VehiclesServed++;
                        booth.State = BoothState.ClosingBarrier;
                        booth.Timer = booth.BarrierSeconds;
                    }
                    break;

                case BoothState.ClosingBarrier:
                    booth.Timer -= dt;
                    booth.BarrierOpen = Clamp01(booth.Timer / booth.BarrierSeconds);
                    if (booth.Timer <= 0)
                    {
                        booth.BarrierOpen = 0;
                        booth.State = BoothState.Idle;
                        TickWaiting(booth, now);
                    }
                    break;
            }
        }

        private void TickWaiting(TollBooth booth, double now)
        {
            if (!booth.IsStaffed || (booth.PendingClose && booth.AssignedVehicles.Count == 0))
            {
                if (booth.AssignedVehicles.Count == 0)
                {
                    booth.State = BoothState.Closed;
                    BoothClosed?.Invoke(booth);
                    return;
                }
                // Unstaffed with vehicles queued cannot happen through the public API, but never strand cars.
                if (!booth.IsStaffed) return;
            }

            Vehicle front = FrontUnpaid(booth);
            if (front == null)
            {
                booth.State = booth.AssignedVehicles.Count > 0 ? BoothState.WaitingForVehicle : BoothState.Idle;
                return;
            }

            booth.State = BoothState.WaitingForVehicle;
            if (front.Distance >= _lanes.StopDistance - StopTolerance && front.Speed <= StoppedSpeed)
            {
                booth.CurrentVehicle = front;
                front.Speed = 0;
                front.State = VehicleState.Paying;
                booth.State = BoothState.ProcessingPayment;
                booth.Timer = booth.ProcessingSeconds * front.Type.ServiceTimeMultiplier;
                _stats.RecordQueueWait(now - front.QueueJoinTime);
            }
        }

        private static Vehicle FrontUnpaid(TollBooth booth)
        {
            foreach (var v in booth.Lane.Vehicles)
            {
                if (!v.BarrierCleared) return v;
            }
            return null;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
