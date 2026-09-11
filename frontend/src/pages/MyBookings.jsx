import { useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { QRCodeSVG } from 'qrcode.react';
import { toast } from 'sonner';
import { motion } from 'framer-motion';
import api from '../services/api';
import { startSignalRConnection } from '../services/signalr';
import {
  QrCode,
  CheckCircle,
  Clock,
  Ban,
  RefreshCw,
} from 'lucide-react';

export default function MyBookings() {
  const queryClient = useQueryClient();

  // Fetch employee bookings
  const {
    data: bookings = [],
    isLoading,
    refetch,
  } = useQuery({
    queryKey: ['myBookings'],
    queryFn: async () => {
      const res = await api.get('/bookings/my');
      return res.data;
    },
  });

  // Listen to SignalR TokenDispensed event
  useEffect(() => {
    let conn = null;
    const setupListener = async () => {
      conn = await startSignalRConnection();
      conn.on('TokenDispensed', (dispensedEvent) => {
        console.log('Token dispensed real-time event:', dispensedEvent);
        queryClient.setQueryData(['myBookings'], (old) => {
          if (!old || !Array.isArray(old)) return old;
          return old.map((b) =>
            b.bookingReference === dispensedEvent.bookingReference
              ? {
                  ...b,
                  status: 2, // Dispensed
                  dispensedAt: dispensedEvent.dispensedAt,
                  dispensedByUserId: dispensedEvent.dispensedBy,
                }
              : b
          );
        });
        toast.info(`Token ${dispensedEvent.bookingReference} marked as DISPENSED at kitchen counter.`);
      });
    };

    setupListener();

    return () => {
      if (conn) {
        conn.off('TokenDispensed');
      }
    };
  }, [queryClient]);

  // Cancel Booking Mutation
  const cancelMutation = useMutation({
    mutationFn: async (token) => {
      const res = await api.post('/bookings/cancel', {
        token,
        reason: 'Cancelled by Employee',
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myBookings'] });
      queryClient.invalidateQueries({ queryKey: ['weeklyMenu'] });
      toast.success('Booking successfully cancelled and slot released.');
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || 'Failed to cancel reservation.');
    },
  });

  const handleCancel = (token) => {
    if (window.confirm(`Are you sure you want to cancel booking token ${token}?`)) {
      cancelMutation.mutate(token);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 1:
        return <span className="badge-status confirmed"><Clock size={12} /> Confirmed (Active)</span>;
      case 2:
        return <span className="badge-status dispensed"><CheckCircle size={12} /> Dispensed</span>;
      case 3:
        return <span className="badge-status cancelled"><Ban size={12} /> Cancelled</span>;
      default:
        return <span className="badge-status">Unknown</span>;
    }
  };

  return (
    <div className="bookings-page-container">
      <div className="page-header">
        <div>
          <h2>Personnel Token Wallet</h2>
          <p>Present your active QR code at the kitchen dispensing terminal for contactless collection</p>
        </div>
        <button onClick={() => refetch()} className="btn-secondary flex-center" style={{ gap: '8px' }}>
          <RefreshCw size={16} /> Refresh
        </button>
      </div>

      {isLoading ? (
        <div className="flex-center" style={{ padding: '80px 0' }}>
          <div className="spinner"></div>
        </div>
      ) : bookings.length === 0 ? (
        <div className="empty-state-box">
          <QrCode size={48} color="var(--text-muted)" style={{ marginBottom: '16px' }} />
          <h3>No Meal Tokens Found</h3>
          <p>You currently do not have any active or past reservations.</p>
          <a href="/#menu" className="btn-primary" style={{ marginTop: '16px' }}>
            Browse Weekly Menu
          </a>
        </div>
      ) : (
        <div className="tokens-grid">
          {bookings.map((booking) => (
            <motion.div
              key={booking.id}
              className={`token-card ${booking.status === 1 ? 'active-token' : 'inactive-token'}`}
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
            >
              <div className="token-card-header">
                <div className="token-ref-box">
                  <span className="token-label">TOKEN REFERENCE</span>
                  <span className="token-code">{booking.bookingReference}</span>
                </div>
                {getStatusBadge(booking.status)}
              </div>

              {/* QR Code Container */}
              <div className="qr-code-wrapper">
                <div className="qr-box">
                  <QRCodeSVG
                    value={`${booking.bookingReference}|${booking.userName}|${booking.mealName}|${booking.scheduledMealDate}`}
                    size={160}
                    level="H"
                    includeMargin={true}
                  />
                </div>
                {booking.status === 2 && (
                  <div className="qr-stamp dispensed">
                    <span>DISPENSED</span>
                  </div>
                )}
                {booking.status === 3 && (
                  <div className="qr-stamp cancelled">
                    <span>CANCELLED</span>
                  </div>
                )}
              </div>

              {/* Token Details */}
              <div className="token-meta-list">
                <div className="meta-row">
                  <span>Meal Item</span>
                  <strong>{booking.mealName}</strong>
                </div>
                <div className="meta-row">
                  <span>Scheduled Date</span>
                  <span>{booking.scheduledMealDate}</span>
                </div>
                <div className="meta-row">
                  <span>Recipient</span>
                  <span>{booking.userName} ({booking.employeeCode})</span>
                </div>
                <div className="meta-row">
                  <span>Amount</span>
                  <span className="meta-price">₹{booking.price}</span>
                </div>
                {booking.dispensedAt && (
                  <div className="meta-row highlight">
                    <span>Dispensed At</span>
                    <span>{new Date(booking.dispensedAt).toLocaleTimeString()}</span>
                  </div>
                )}
              </div>

              {/* Actions */}
              {booking.status === 1 && (
                <div className="token-actions">
                  <button
                    onClick={() => handleCancel(booking.bookingReference)}
                    className="btn-cancel"
                    disabled={cancelMutation.isPending}
                  >
                    Cancel Reservation
                  </button>
                </div>
              )}
            </motion.div>
          ))}
        </div>
      )}
    </div>
  );
}
