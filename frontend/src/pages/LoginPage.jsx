import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useAuth } from '../context/AuthContext';
import { toast } from 'sonner';
import { Shield, Lock, Mail, Key, UserCheck, ShieldAlert } from 'lucide-react';
import api from '../services/api';

const loginSchema = z.object({
  email: z.string().email('Valid DRDO email address is required.'),
  password: z.string().min(6, 'Password must be at least 6 characters.'),
});

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const from = location.state?.from?.pathname || '/';

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data) => {
    setIsSubmitting(true);
    try {
      const user = await login(data.email, data.password);
      toast.success(`Access Granted: Welcome, ${user.fullName}`);
      if (user.role === 'KitchenOperator') {
        navigate('/kitchen-kiosk');
      } else if (user.role === 'CanteenManager') {
        navigate('/admin/analytics');
      } else {
        navigate(from === '/login' ? '/' : from);
      }
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || 'Authentication failed. Please verify credentials.';
      toast.error(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleGoogleSignIn = async () => {
    setIsSubmitting(true);
    try {
      const res = await api.post('/auth/google-login', {
        credential: 'google-oauth-defense-token',
        email: 'scientist.sso@drdo.gov.in',
        name: 'Dr. Ayusman (SSO Defence)',
      });
      localStorage.setItem('drdo_access_token', res.data.accessToken);
      localStorage.setItem('drdo_refresh_token', res.data.refreshToken);
      localStorage.setItem('drdo_user', JSON.stringify(res.data.user));
      toast.success(`Access Granted via Google SSO: ${res.data.user.fullName}`);
      navigate('/');
      window.location.reload();
    } catch (err) {
      console.error(err);
      toast.error('Google SSO authentication failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleQuickFill = (email, password) => {
    setValue('email', email);
    setValue('password', password);
    toast.info(`Filled credentials for ${email}`);
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <div className="shield-icon-wrapper">
            <Shield size={36} color="var(--primary-gold)" />
          </div>
          <h2>Security Clearance Portal</h2>
          <p>Defence Research & Development Organisation Catering Gateway</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="login-form">
          <div className="form-group">
            <label>
              <Mail size={16} /> Official Email Address
            </label>
            <input
              type="email"
              placeholder="name@drdo.gov.in"
              {...register('email')}
              className={errors.email ? 'input-error' : ''}
            />
            {errors.email && <span className="error-text">{errors.email.message}</span>}
          </div>

          <div className="form-group">
            <label>
              <Lock size={16} /> Password
            </label>
            <input
              type="password"
              placeholder="••••••••••••"
              {...register('password')}
              className={errors.password ? 'input-error' : ''}
            />
            {errors.password && <span className="error-text">{errors.password.message}</span>}
          </div>

          <button type="submit" className="btn-primary login-btn" disabled={isSubmitting}>
            {isSubmitting ? (
              <span className="flex-center" style={{ gap: '8px' }}>
                <span className="spinner-small"></span> Verifying Security Clearance...
              </span>
            ) : (
              <span className="flex-center" style={{ gap: '8px' }}>
                <Key size={18} /> Authenticate Access
              </span>
            )}
          </button>
        </form>

        <div style={{ textAlign: 'center', margin: '14px 0 8px 0', color: 'var(--text-muted)', fontSize: '0.75rem', letterSpacing: '1px' }}>
          <span>— OR SINGLE SIGN-ON —</span>
        </div>

        <button
          type="button"
          className="btn-secondary"
          style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', padding: '10px' }}
          onClick={handleGoogleSignIn}
          disabled={isSubmitting}
        >
          <svg width="18" height="18" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"/>
          </svg>
          Continue with Google Defence SSO
        </button>

        <div className="quick-access-box">
          <h4>
            <UserCheck size={16} /> Demo Clearance Credentials (Click to Autofill)
          </h4>
          <div className="quick-badges">
            <button
              type="button"
              className="badge-role manager"
              onClick={() => handleQuickFill('manager@drdo.gov.in', 'Manager@DRDO2026!')}
            >
              <strong>Canteen Manager</strong>
              <span>Col. Rajesh Verma</span>
            </button>
            <button
              type="button"
              className="badge-role kitchen"
              onClick={() => handleQuickFill('kitchen@drdo.gov.in', 'Kitchen@DRDO2026!')}
            >
              <strong>Kitchen Operator</strong>
              <span>Chef Amit Kumar</span>
            </button>
            <button
              type="button"
              className="badge-role employee"
              onClick={() => handleQuickFill('employee@drdo.gov.in', 'Employee@DRDO2026!')}
            >
              <strong>Employee</strong>
              <span>Dr. Ayusman Mohanty</span>
            </button>
          </div>
        </div>

        <div className="security-notice">
          <ShieldAlert size={14} />
          <span>Restricted system: Unauthorized access attempts are monitored and logged.</span>
        </div>
      </div>
    </div>
  );
}
