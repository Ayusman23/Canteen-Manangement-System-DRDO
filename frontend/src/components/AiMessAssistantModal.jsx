import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, X, Bot, Apple, Zap, HeartPulse, Send, RefreshCw } from 'lucide-react';
import api from '../services/api';
import { toast } from 'sonner';

export default function AiMessAssistantModal({ isOpen, onClose }) {
  const [prompt, setPrompt] = useState('');
  const [dietaryGoal, setDietaryGoal] = useState('Energy & Focus');
  const [loading, setLoading] = useState(false);
  const [aiResponse, setAiResponse] = useState(null);

  const goalOptions = [
    { label: 'Energy & Focus', icon: Zap },
    { label: 'High Protein', icon: HeartPulse },
    { label: 'Nutritious & Light', icon: Apple },
  ];

  const handleAskAi = async (e) => {
    if (e) e.preventDefault();
    setLoading(true);
    try {
      const res = await api.post('/ai/mess-assistant', {
        prompt: prompt || 'What should I eat from today\'s menu to stay energized and focused throughout the day?',
        dietaryGoal,
      });

      setAiResponse(res.data);
    } catch (err) {
      console.error(err);
      toast.error('Could not retrieve AI Mess recommendation. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="modal-backdrop" onClick={onClose}>
        <motion.div
          className="modal-card"
          style={{ maxWidth: '640px', width: '92%' }}
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="modal-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border-color)', paddingBottom: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <div style={{ background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)', padding: '8px', borderRadius: '10px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Bot size={22} color="#ffffff" />
              </div>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.2rem', fontWeight: '700', color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                  DRDO AI Mess &amp; Nutrition Advisor
                  <span style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '9999px', background: 'rgba(59, 130, 246, 0.2)', color: '#60a5fa', border: '1px solid #3b82f6' }}>
                    Gemini Powered
                  </span>
                </h3>
                <p style={{ margin: 0, fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  Intelligent dietary guidance customized to today's active menu
                </p>
              </div>
            </div>
            <button className="btn-icon" onClick={onClose} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--text-muted)' }}>
              <X size={20} />
            </button>
          </div>

          <div style={{ padding: '1.25rem 0' }}>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ fontSize: '0.85rem', fontWeight: '600', color: 'var(--text-secondary)', display: 'block', marginBottom: '0.5rem' }}>
                Select Your Operational Focus:
              </label>
              <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                {goalOptions.map((opt) => {
                  const Icon = opt.icon;
                  const isSelected = dietaryGoal === opt.label;
                  return (
                    <button
                      key={opt.label}
                      type="button"
                      onClick={() => setDietaryGoal(opt.label)}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '6px',
                        padding: '6px 14px',
                        borderRadius: '8px',
                        fontSize: '0.8rem',
                        fontWeight: '600',
                        cursor: 'pointer',
                        border: isSelected ? '1px solid #3b82f6' : '1px solid var(--border-color)',
                        background: isSelected ? 'rgba(59, 130, 246, 0.2)' : 'rgba(255, 255, 255, 0.03)',
                        color: isSelected ? '#93c5fd' : 'var(--text-secondary)',
                        transition: 'all 0.2s',
                      }}
                    >
                      <Icon size={14} />
                      {opt.label}
                    </button>
                  );
                })}
              </div>
            </div>

            <form onSubmit={handleAskAi} style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.25rem' }}>
              <input
                type="text"
                className="form-input"
                placeholder="E.g., High protein meal for afternoon lab work, or allergy check..."
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                style={{ flex: 1 }}
              />
              <button
                type="submit"
                className="btn btn-primary"
                disabled={loading}
                style={{ display: 'flex', alignItems: 'center', gap: '6px', whiteSpace: 'nowrap' }}
              >
                {loading ? <RefreshCw className="animate-spin" size={16} /> : <Send size={16} />}
                {loading ? 'Consulting...' : 'Ask AI'}
              </button>
            </form>

            {aiResponse && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                style={{
                  background: 'rgba(15, 23, 42, 0.65)',
                  border: '1px solid rgba(59, 130, 246, 0.3)',
                  borderRadius: '12px',
                  padding: '1.25rem',
                  fontSize: '0.9rem',
                  lineHeight: '1.6',
                  color: 'var(--text-primary)',
                  maxHeight: '280px',
                  overflowY: 'auto',
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem', borderBottom: '1px solid rgba(255,255,255,0.06)', paddingBottom: '0.5rem' }}>
                  <span style={{ fontSize: '0.75rem', color: '#94a3b8', display: 'flex', alignItems: 'center', gap: '4px' }}>
                    <Sparkles size={13} color="#60a5fa" /> Verified Guidance: {aiResponse.Provider}
                  </span>
                  <span style={{ fontSize: '0.75rem', color: '#64748b' }}>
                    Dietary Focus: {dietaryGoal}
                  </span>
                </div>
                <div style={{ whiteSpace: 'pre-wrap' }}>
                  {aiResponse.Response}
                </div>
              </motion.div>
            )}

            {!aiResponse && !loading && (
              <div style={{ textAlign: 'center', padding: '1.5rem', background: 'rgba(255, 255, 255, 0.02)', borderRadius: '10px', border: '1px dashed var(--border-color)' }}>
                <Sparkles size={28} color="#60a5fa" style={{ margin: '0 auto 8px auto', display: 'block' }} />
                <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  Click <strong>Ask AI</strong> to receive tailored dietary recommendations matched against today's mess menu.
                </p>
              </div>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', borderTop: '1px solid var(--border-color)', paddingTop: '1rem' }}>
            <button className="btn btn-secondary" onClick={onClose}>
              Dismiss
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
