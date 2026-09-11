import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { motion, AnimatePresence } from 'framer-motion';
import { Link, useNavigate } from 'react-router-dom';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';
import { startSignalRConnection } from '../services/signalr';
import {
  Utensils,
  Clock,
  CheckCircle2,
  AlertCircle,
  X,
  Calendar,
  Sparkles,
  QrCode,
  ShieldCheck,
  ChevronRight,
  Flame,
  Coffee,
  CreditCard,
} from 'lucide-react';
import AiMessAssistantModal from '../components/AiMessAssistantModal';

const bookingSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters.'),
  employeeCode: z.string().min(3, 'Employee code is required.'),
  mealType: z.enum(['Normal', 'Special', 'Item']),
  scheduleId: z.string().uuid(),
  day: z.string(),
});

export default function EmployeePortal() {
  const { user, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [bookingSuccessData, setBookingSuccessData] = useState(null);
  const [isAiModalOpen, setIsAiModalOpen] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState('payroll');

  // Fetch Weekly Menu via TanStack Query
  const {
    data: weeklyMenu = [],
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['weeklyMenu'],
    queryFn: async () => {
      const res = await api.get('/menu/weekly');
      return res.data;
    },
    staleTime: 30000,
  });

  // SignalR Real-Time Inventory Updates
  useEffect(() => {
    let conn = null;
    const initSignalR = async () => {
      conn = await startSignalRConnection();
      conn.on('MealAvailabilityUpdated', (data) => {
        console.log('Real-time meal availability update received:', data);
        queryClient.setQueryData(['weeklyMenu'], (oldData) => {
          if (!oldData || !Array.isArray(oldData)) return oldData;
          return oldData.map((day) => {
            const updateSchedule = (sched) => {
              if (sched && sched.id === data.scheduleId) {
                return {
                  ...sched,
                  currentBookingsCount: data.currentBookings,
                  remainingCapacity: data.remainingCapacity,
                  maxCapacity: data.maxCapacity,
                };
              }
              return sched;
            };
            return {
              ...day,
              normalMeal: updateSchedule(day.normalMeal),
              specialMeal: updateSchedule(day.specialMeal),
              addonItem: updateSchedule(day.addonItem),
            };
          });
        });
      });
    };

    initSignalR();

    return () => {
      if (conn) {
        conn.off('MealAvailabilityUpdated');
      }
    };
  }, [queryClient]);

  // Form handling
  const {
    register,
    handleSubmit,
    setValue,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(bookingSchema),
    defaultValues: {
      name: user?.fullName || '',
      employeeCode: user?.employeeCode || '',
      mealType: 'Normal',
      scheduleId: '',
      day: '',
    },
  });

  useEffect(() => {
    if (user) {
      setValue('name', user.fullName);
      setValue('employeeCode', user.employeeCode || 'DRDO-STAFF');
    }
  }, [user, setValue]);

  const openBookingModal = (dayName, schedule) => {
    if (!isAuthenticated) {
      toast.info('Please log in with your DRDO credentials to book meals.');
      navigate('/login');
      return;
    }

    if (schedule.isCutoffPassed) {
      toast.error('Booking cutoff time has passed for this meal.');
      return;
    }

    if (schedule.remainingCapacity <= 0) {
      toast.error('Capacity full: No more reservations available for this slot.');
      return;
    }

    setSelectedSlot({ dayName, schedule });
    setValue('day', dayName);
    setValue('mealType', schedule.mealType === 1 ? 'Normal' : schedule.mealType === 2 ? 'Special' : 'Item');
    setValue('scheduleId', schedule.id);
  };

  // Booking Mutation
  const bookingMutation = useMutation({
    mutationFn: async (formData) => {
      const res = await api.post('/bookings', {
        name: formData.name,
        mealType: formData.mealType,
        day: formData.day,
        scheduleId: formData.scheduleId,
        employeeCode: formData.employeeCode,
      });
      return res.data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['weeklyMenu'] });
      queryClient.invalidateQueries({ queryKey: ['myBookings'] });
      setBookingSuccessData(data);
      setSelectedSlot(null);
      reset();
      toast.success(data.message || 'Meal slot secured successfully!');
    },
    onError: (err) => {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Failed to complete reservation.';
      toast.error(msg);
    },
  });

  const onBookingSubmit = (data) => {
    bookingMutation.mutate(data);
  };

  return (
    <div className="employee-portal">
      {/* Hero Section */}
      <section className="hero-defense">
        <div className="hero-content">
          <div className="defense-badge">
            <ShieldCheck size={16} /> DEFENCE RESEARCH & DEVELOPMENT ESTABLISHMENT
          </div>
          <h1>
            Automated Culinary Operations & <span>Real-Time Logistics</span>
          </h1>
          <p>
            Zero-wait dining pipeline for scientists, technical staff, and administrative personnel with instant QR
            token dispatching and dynamic inventory balancing.
          </p>

          <div className="hero-stats-strip">
            <div className="hero-stat-item">
              <span className="stat-value">100%</span>
              <span className="stat-label">Hygienic Standards</span>
            </div>
            <div className="hero-stat-separator"></div>
            <div className="hero-stat-item">
              <span className="stat-value">&lt; 3s</span>
              <span className="stat-label">QR Token Issuance</span>
            </div>
            <div className="hero-stat-separator"></div>
            <div className="hero-stat-item">
              <span className="stat-value">Zero</span>
              <span className="stat-label">Overbooking Spikes</span>
            </div>
          </div>

          <div className="hero-actions" style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
            {isAuthenticated && (
              <Link to="/my-bookings" className="btn-primary">
                <QrCode size={18} /> View My Scannable Tokens
              </Link>
            )}
            <button
              type="button"
              className="btn-secondary"
              onClick={() => setIsAiModalOpen(true)}
              style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}
            >
              <Sparkles size={18} color="#60a5fa" /> AI Mess Nutritionist
            </button>
          </div>
        </div>
      </section>

      {/* Success Notification Card */}
      <AnimatePresence>
        {bookingSuccessData && (
          <motion.div
            className="booking-success-banner"
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
          >
            <div className="success-banner-content">
              <CheckCircle2 size={32} color="var(--success)" />
              <div className="success-banner-text">
                <h3>{bookingSuccessData.message}</h3>
                <p>
                  Security Token: <strong>{bookingSuccessData.token}</strong> — Meal:{' '}
                  <span>{bookingSuccessData.details?.mealName}</span> (₹{bookingSuccessData.details?.price})
                </p>
              </div>
              <div className="success-banner-actions">
                <Link to="/my-bookings" className="btn-secondary">
                  Access Token Wallet <ChevronRight size={16} />
                </Link>
                <button className="btn-icon-close" onClick={() => setBookingSuccessData(null)}>
                  <X size={18} />
                </button>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Weekly Menu Section */}
      <section className="menu-container" id="menu">
        <div className="section-header">
          <div>
            <h2>Weekly Dynamic Catering Schedule</h2>
            <p>Real-time slot availability automatically updated via SignalR broadcast</p>
          </div>
          <button onClick={() => refetch()} className="btn-refresh" title="Refresh Schedule">
            Sync Live
          </button>
        </div>

        {isLoading ? (
          <div className="flex-center" style={{ padding: '80px 0' }}>
            <div className="spinner"></div>
          </div>
        ) : isError ? (
          <div className="error-card">
            <AlertCircle size={24} />
            <p>Failed to synchronize weekly menu. Please check backend connection.</p>
            <button onClick={() => refetch()} className="btn-secondary">
              Retry
            </button>
          </div>
        ) : (
          <div className="weekly-schedule-grid">
            {weeklyMenu.map((dayItem, idx) => (
              <motion.div
                key={dayItem.day}
                className="day-column-card"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.05 }}
              >
                <div className="day-card-header">
                  <span className="day-name">{dayItem.day}</span>
                  <span className="day-date">{dayItem.date}</span>
                </div>

                <div className="day-meals-list">
                  {/* Normal Thali Slot */}
                  {dayItem.normalMeal && (
                    <div className="meal-slot-item">
                      <div className="slot-title-row">
                        <span className="badge-meal normal">Normal Thali</span>
                        <span className="slot-price">₹{dayItem.normalMeal.price}</span>
                      </div>
                      <h4 className="slot-item-name">{dayItem.normalMeal.menuItemName}</h4>
                      <p className="slot-item-desc">{dayItem.normalMeal.description || 'Standard nutritious meal'}</p>

                      {/* Capacity Bar */}
                      <div className="capacity-bar-container">
                        <div className="capacity-bar-track">
                          <div
                            className={`capacity-bar-fill ${
                              dayItem.normalMeal.remainingCapacity <= 10 ? 'critical' : ''
                            }`}
                            style={{
                              width: `${Math.min(
                                100,
                                (dayItem.normalMeal.currentBookingsCount / dayItem.normalMeal.maxCapacity) * 100
                              )}%`,
                            }}
                          ></div>
                        </div>
                        <div className="capacity-meta">
                          <span>{dayItem.normalMeal.remainingCapacity} slots left</span>
                          <span>Cap: {dayItem.normalMeal.maxCapacity}</span>
                        </div>
                      </div>

                      <button
                        className="btn-book-slot"
                        disabled={dayItem.normalMeal.remainingCapacity <= 0 || dayItem.normalMeal.isCutoffPassed}
                        onClick={() => openBookingModal(dayItem.day, dayItem.normalMeal)}
                      >
                        {dayItem.normalMeal.isCutoffPassed
                          ? 'Cutoff Passed'
                          : dayItem.normalMeal.remainingCapacity <= 0
                          ? 'Capacity Full'
                          : `Reserve (₹${dayItem.normalMeal.price})`}
                      </button>
                    </div>
                  )}

                  {/* Special Meal Slot */}
                  {dayItem.specialMeal && (
                    <div className="meal-slot-item highlight-special">
                      <div className="slot-title-row">
                        <span className="badge-meal special">
                          <Sparkles size={12} /> Chef Special
                        </span>
                        <span className="slot-price">₹{dayItem.specialMeal.price}</span>
                      </div>
                      <h4 className="slot-item-name">{dayItem.specialMeal.menuItemName}</h4>
                      <p className="slot-item-desc">{dayItem.specialMeal.description || 'Deluxe preparation'}</p>

                      {/* Capacity Bar */}
                      <div className="capacity-bar-container">
                        <div className="capacity-bar-track">
                          <div
                            className={`capacity-bar-fill special ${
                              dayItem.specialMeal.remainingCapacity <= 10 ? 'critical' : ''
                            }`}
                            style={{
                              width: `${Math.min(
                                100,
                                (dayItem.specialMeal.currentBookingsCount / dayItem.specialMeal.maxCapacity) * 100
                              )}%`,
                            }}
                          ></div>
                        </div>
                        <div className="capacity-meta">
                          <span>{dayItem.specialMeal.remainingCapacity} slots left</span>
                          <span>Cap: {dayItem.specialMeal.maxCapacity}</span>
                        </div>
                      </div>

                      <button
                        className="btn-book-slot special-btn"
                        disabled={dayItem.specialMeal.remainingCapacity <= 0 || dayItem.specialMeal.isCutoffPassed}
                        onClick={() => openBookingModal(dayItem.day, dayItem.specialMeal)}
                      >
                        {dayItem.specialMeal.isCutoffPassed
                          ? 'Cutoff Passed'
                          : dayItem.specialMeal.remainingCapacity <= 0
                          ? 'Capacity Full'
                          : `Reserve (₹${dayItem.specialMeal.price})`}
                      </button>
                    </div>
                  )}

                  {/* Addon / Item Slot */}
                  {dayItem.addonItem && (
                    <div className="meal-slot-item addon">
                      <div className="slot-title-row">
                        <span className="badge-meal addon">
                          <Coffee size={12} /> Add-on
                        </span>
                        <span className="slot-price">₹{dayItem.addonItem.price}</span>
                      </div>
                      <h4 className="slot-item-name">{dayItem.addonItem.menuItemName}</h4>

                      <button
                        className="btn-book-slot addon-btn"
                        disabled={dayItem.addonItem.remainingCapacity <= 0 || dayItem.addonItem.isCutoffPassed}
                        onClick={() => openBookingModal(dayItem.day, dayItem.addonItem)}
                      >
                        Add-on (₹{dayItem.addonItem.price})
                      </button>
                    </div>
                  )}
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </section>

      {/* Booking Confirmation Modal */}
      <AnimatePresence>
        {selectedSlot && (
          <div className="modal-overlay" onClick={() => setSelectedSlot(null)}>
            <motion.div
              className="modal-card"
              onClick={(e) => e.stopPropagation()}
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
            >
              <div className="modal-header">
                <div>
                  <h3>Confirm Meal Reservation</h3>
                  <p>
                    {selectedSlot.dayName} • {selectedSlot.schedule.menuItemName}
                  </p>
                </div>
                <button className="btn-icon" onClick={() => setSelectedSlot(null)}>
                  <X size={20} />
                </button>
              </div>

              <form onSubmit={handleSubmit(onBookingSubmit)} className="modal-form">
                <div className="form-group">
                  <label>Full Name</label>
                  <input type="text" {...register('name')} className={errors.name ? 'input-error' : ''} />
                  {errors.name && <span className="error-text">{errors.name.message}</span>}
                </div>

                <div className="form-group">
                  <label>DRDO Employee Code</label>
                  <input
                    type="text"
                    {...register('employeeCode')}
                    className={errors.employeeCode ? 'input-error' : ''}
                  />
                  {errors.employeeCode && <span className="error-text">{errors.employeeCode.message}</span>}
                </div>

                <div className="order-summary-box">
                  <div className="summary-row">
                    <span>Scheduled Item</span>
                    <strong>{selectedSlot.schedule.menuItemName}</strong>
                  </div>
                  <div className="summary-row">
                    <span>Category</span>
                    <span>{selectedSlot.schedule.mealType === 1 ? 'Normal Thali' : 'Special Meal'}</span>
                  </div>
                  <div className="summary-row">
                    <span>Price</span>
                    <span className="summary-price">₹{selectedSlot.schedule.price}</span>
                  </div>
                  <div className="summary-row">
                    <span>Cutoff Window</span>
                    <span>Before {selectedSlot.schedule.cutoffTime}</span>
                  </div>
                </div>

                <div className="form-group" style={{ marginTop: '12px' }}>
                  <label style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Settlement Preference</label>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginTop: '4px' }}>
                    <button
                      type="button"
                      onClick={() => setPaymentMethod('payroll')}
                      style={{
                        padding: '8px',
                        borderRadius: '6px',
                        border: paymentMethod === 'payroll' ? '1px solid #3b82f6' : '1px solid var(--border-color)',
                        background: paymentMethod === 'payroll' ? 'rgba(59, 130, 246, 0.15)' : 'transparent',
                        color: paymentMethod === 'payroll' ? '#93c5fd' : 'var(--text-secondary)',
                        fontSize: '0.8rem',
                        fontWeight: '600',
                        cursor: 'pointer'
                      }}
                    >
                      Mess Payroll Debit
                    </button>
                    <button
                      type="button"
                      onClick={() => setPaymentMethod('online')}
                      style={{
                        padding: '8px',
                        borderRadius: '6px',
                        border: paymentMethod === 'online' ? '1px solid #3b82f6' : '1px solid var(--border-color)',
                        background: paymentMethod === 'online' ? 'rgba(59, 130, 246, 0.15)' : 'transparent',
                        color: paymentMethod === 'online' ? '#93c5fd' : 'var(--text-secondary)',
                        fontSize: '0.8rem',
                        fontWeight: '600',
                        cursor: 'pointer'
                      }}
                    >
                      Razorpay Online
                    </button>
                  </div>
                </div>

                <button
                  type="submit"
                  className="btn-primary"
                  style={{ width: '100%', marginTop: '16px' }}
                  disabled={bookingMutation.isPending}
                >
                  {bookingMutation.isPending ? 'Securing Slot & Generating Token...' : 'Confirm Reservation'}
                </button>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
      <AiMessAssistantModal isOpen={isAiModalOpen} onClose={() => setIsAiModalOpen(false)} />
    </div>
  );
}
