import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import { toast } from 'sonner';
import api from '../services/api';
import {
  TrendingUp,
  Clock,
  PieChart as PieIcon,
  ShieldAlert,
  Users,
  CheckCircle,
  IndianRupee,
  Layers,
  Settings,
  RefreshCw,
} from 'lucide-react';

const COLORS = ['#3b82f6', '#f59e0b', '#10b981', '#ef4444'];

export default function AdminAnalytics() {
  const queryClient = useQueryClient();
  const [selectedSchedule, setSelectedSchedule] = useState(null);
  const [newCapacity, setNewCapacity] = useState(150);

  // Fetch Analytics from API
  const {
    data: analytics,
    isLoading,
    refetch,
  } = useQuery({
    queryKey: ['canteenAnalytics'],
    queryFn: async () => {
      const res = await api.get('/analytics');
      return res.data;
    },
    refetchInterval: 15000, // Auto-refresh every 15s
  });

  // Fetch Weekly Schedules for Manager capacity configuration
  const { data: weeklyMenu = [] } = useQuery({
    queryKey: ['weeklyMenu'],
    queryFn: async () => {
      const res = await api.get('/menu/weekly');
      return res.data;
    },
  });

  // Capacity update mutation
  const capacityMutation = useMutation({
    mutationFn: async ({ scheduleId, capacity }) => {
      const res = await api.put(`/menu/schedules/${scheduleId}/capacity`, {
        newCapacity: capacity,
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['weeklyMenu'] });
      queryClient.invalidateQueries({ queryKey: ['canteenAnalytics'] });
      setSelectedSchedule(null);
      toast.success('Capacity updated and broadcast to all connected clients via SignalR.');
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || 'Failed to update capacity.');
    },
  });

  const handleUpdateCapacity = (e) => {
    e.preventDefault();
    if (!selectedSchedule) return;
    capacityMutation.mutate({
      scheduleId: selectedSchedule.id,
      capacity: parseInt(newCapacity, 10),
    });
  };

  if (isLoading) {
    return (
      <div className="flex-center" style={{ minHeight: '60vh' }}>
        <div className="spinner"></div>
      </div>
    );
  }

  const trendsData = analytics?.bookingTrends || [];
  const peakHoursData = analytics?.peakHoursDistribution || [];
  const mealRatios = analytics?.mealDemandRatios || [];

  return (
    <div className="analytics-page-container">
      {/* Header */}
      <div className="page-header">
        <div>
          <h2>Operational Command & Logistics Analytics</h2>
          <p>Real-time telemetry, demand forecasting, and meal inventory management</p>
        </div>
        <button onClick={() => refetch()} className="btn-secondary flex-center" style={{ gap: '8px' }}>
          <RefreshCw size={16} /> Refresh Metrics
        </button>
      </div>

      {/* KPI Cards Strip */}
      <div className="kpi-grid">
        <div className="kpi-card">
          <div className="kpi-icon-box blue">
            <Users size={22} />
          </div>
          <div className="kpi-info">
            <span className="kpi-label">Today's Reservations</span>
            <span className="kpi-value">{analytics?.totalBookingsToday ?? 0}</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon-box green">
            <CheckCircle size={22} />
          </div>
          <div className="kpi-info">
            <span className="kpi-label">Meals Dispensed</span>
            <span className="kpi-value">{analytics?.totalDispensedToday ?? 0}</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon-box amber">
            <Clock size={22} />
          </div>
          <div className="kpi-info">
            <span className="kpi-label">Active Tokens in Queue</span>
            <span className="kpi-value">{analytics?.totalActiveTokens ?? 0}</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon-box gold">
            <IndianRupee size={22} />
          </div>
          <div className="kpi-info">
            <span className="kpi-label">Gross Revenue Today</span>
            <span className="kpi-value">₹{analytics?.totalRevenueToday?.toLocaleString() ?? 0}</span>
          </div>
        </div>
      </div>

      {/* Analytics Charts Grid */}
      <div className="charts-grid">
        {/* Daily Booking Trend Area Chart */}
        <div className="chart-card">
          <div className="chart-header">
            <h4>
              <TrendingUp size={18} /> 7-Day Booking Demand Trends
            </h4>
            <span className="chart-subtitle">Aggregated normal vs special volume</span>
          </div>
          <div className="chart-wrapper">
            <ResponsiveContainer width="100%" height={260}>
              <AreaChart data={trendsData} margin={{ top: 10, right: 20, left: 0, bottom: 0 }}>
                <defs>
                  <linearGradient id="colorTotal" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="colorSpecial" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#f59e0b" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#f59e0b" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                <XAxis dataKey="date" stroke="#94a3b8" />
                <YAxis stroke="#94a3b8" />
                <Tooltip
                  contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '8px' }}
                />
                <Legend />
                <Area type="monotone" dataKey="total" name="Total Meals" stroke="#3b82f6" fillOpacity={1} fill="url(#colorTotal)" />
                <Area type="monotone" dataKey="specialMeals" name="Special Thali" stroke="#f59e0b" fillOpacity={1} fill="url(#colorSpecial)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Peak Pickup Hours Bar Chart */}
        <div className="chart-card">
          <div className="chart-header">
            <h4>
              <Clock size={18} /> Peak Counter Pickup Hours
            </h4>
            <span className="chart-subtitle">Kitchen collection traffic distribution</span>
          </div>
          <div className="chart-wrapper">
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={peakHoursData} margin={{ top: 10, right: 20, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                <XAxis dataKey="hourWindow" stroke="#94a3b8" />
                <YAxis stroke="#94a3b8" />
                <Tooltip
                  contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '8px' }}
                />
                <Bar dataKey="pickupsCount" name="Meals Dispensed" fill="#10b981" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Meal Demand Ratio Donut Chart */}
        <div className="chart-card">
          <div className="chart-header">
            <h4>
              <PieIcon size={18} /> Meal Type Demand Ratios
            </h4>
            <span className="chart-subtitle">Proportion of Normal vs Special preferences</span>
          </div>
          <div className="chart-wrapper flex-center">
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie
                  data={mealRatios}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={90}
                  paddingAngle={5}
                  dataKey="value"
                  label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                >
                  {mealRatios.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '8px' }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Cancellation Statistics Card */}
        <div className="chart-card">
          <div className="chart-header">
            <h4>
              <ShieldAlert size={18} /> Cancellation & Attrition Safeguards
            </h4>
            <span className="chart-subtitle">Meal release and wastage mitigation</span>
          </div>
          <div className="cancellation-metrics-box">
            <div className="cancellation-stat-row">
              <span>Total Released / Cancelled</span>
              <strong>{analytics?.cancellationStats?.totalCancelled ?? 0} Meals</strong>
            </div>
            <div className="cancellation-stat-row">
              <span>Cancellation Rate</span>
              <strong className="rate-badge">{analytics?.cancellationStats?.cancellationRatePercentage ?? 0}%</strong>
            </div>
            <div className="cancellation-stat-row">
              <span>Primary Reason</span>
              <span>{analytics?.cancellationStats?.topCancellationReason ?? 'N/A'}</span>
            </div>
            <div className="attrition-notice">
              All cancelled meals are automatically freed into the live inventory buffer in real-time, preventing
              food wastage across DRDO messes.
            </div>
          </div>
        </div>
      </div>

      {/* Schedule Capacity & Inventory Controls */}
      <div className="capacity-manager-card">
        <div className="chart-header">
          <h4>
            <Settings size={18} /> Daily Schedule Quotas & Capacity Limits
          </h4>
          <span className="chart-subtitle">
            Configure meal caps to control inventory and prevent mess overcrowding
          </span>
        </div>

        <div className="schedules-table-wrapper">
          <table className="schedules-table">
            <thead>
              <tr>
                <th>Day</th>
                <th>Category</th>
                <th>Item Name</th>
                <th>Price</th>
                <th>Booked / Max Capacity</th>
                <th>Cutoff Time</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {weeklyMenu.map((dayItem) =>
                [dayItem.normalMeal, dayItem.specialMeal].filter(Boolean).map((sched) => (
                  <tr key={sched.id}>
                    <td><strong>{dayItem.day}</strong></td>
                    <td>
                      <span className={`badge-meal ${sched.mealType === 1 ? 'normal' : 'special'}`}>
                        {sched.mealType === 1 ? 'Normal' : 'Special'}
                      </span>
                    </td>
                    <td>{sched.menuItemName}</td>
                    <td>₹{sched.price}</td>
                    <td>
                      <div className="table-capacity-indicator">
                        <span>{sched.currentBookingsCount} / {sched.maxCapacity}</span>
                        <div className="progress-tiny">
                          <div
                            className="progress-tiny-bar"
                            style={{
                              width: `${Math.min(100, (sched.currentBookingsCount / sched.maxCapacity) * 100)}%`,
                            }}
                          ></div>
                        </div>
                      </div>
                    </td>
                    <td>{sched.cutoffTime}</td>
                    <td>
                      <button
                        className="btn-table-action"
                        onClick={() => {
                          setSelectedSchedule(sched);
                          setNewCapacity(sched.maxCapacity);
                        }}
                      >
                        Adjust Cap
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Adjust Capacity Modal */}
      {selectedSchedule && (
        <div className="modal-overlay" onClick={() => setSelectedSchedule(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h3>Adjust Meal Quota Capacity</h3>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '16px' }}>
              {selectedSchedule.dayOfWeek} • {selectedSchedule.menuItemName}
            </p>

            <form onSubmit={handleUpdateCapacity}>
              <div className="form-group">
                <label>Maximum Allowed Reservations</label>
                <input
                  type="number"
                  min={selectedSchedule.currentBookingsCount}
                  value={newCapacity}
                  onChange={(e) => setNewCapacity(e.target.value)}
                  required
                />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  Current confirmed bookings: {selectedSchedule.currentBookingsCount} (Capacity cannot be lower)
                </span>
              </div>

              <div style={{ display: 'flex', gap: '12px', marginTop: '20px' }}>
                <button type="submit" className="btn-primary" disabled={capacityMutation.isPending}>
                  {capacityMutation.isPending ? 'Updating...' : 'Save & Broadcast'}
                </button>
                <button type="button" className="btn-secondary" onClick={() => setSelectedSchedule(null)}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
