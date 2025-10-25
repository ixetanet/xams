# You Might Not Need an Effect

**Key Principle**: Effects are an escape hatch for synchronizing with external systems. For most data transformations and UI logic, you DON'T need useEffect.

## ❌ ANTI-PATTERNS: When NOT to Use useEffect

### 1. Transforming Data for Rendering
**DON'T** use effects to calculate values that can be computed during render.

```javascript
// ❌ BAD: Unnecessary effect
const [fullName, setFullName] = useState('');
useEffect(() => {
  setFullName(firstName + ' ' + lastName);
}, [firstName, lastName]);

// ✅ GOOD: Calculate during render
const fullName = firstName + ' ' + lastName;
```

### 2. Caching Expensive Calculations
**DON'T** use effects for memoization.

```javascript
// ❌ BAD: Effect for caching
const [visibleTodos, setVisibleTodos] = useState([]);
useEffect(() => {
  setVisibleTodos(getFilteredTodos(todos, filter));
}, [todos, filter]);

// ✅ GOOD: Use useMemo
const visibleTodos = useMemo(() =>
  getFilteredTodos(todos, filter),
  [todos, filter]
);
```

### 3. Handling User Events
**DON'T** use effects for user interactions.

```javascript
// ❌ BAD: Effect for form submission
const [submitted, setSubmitted] = useState(false);
useEffect(() => {
  if (submitted) {
    post('/api/register', { firstName, lastName });
  }
}, [submitted, firstName, lastName]);

// ✅ GOOD: Use event handler
function handleSubmit(e) {
  e.preventDefault();
  post('/api/register', { firstName, lastName });
}
```

### 4. Resetting State on Prop Change
**DON'T** use effects to reset component state.

```javascript
// ❌ BAD: Effect to reset state
useEffect(() => {
  setComment('');
}, [userId]);

// ✅ GOOD: Use key prop to reset component
<ProfilePage key={userId} userId={userId} />
```

### 5. Chaining Effects that Update State
**DON'T** create cascading effects that trigger each other.

```javascript
// ❌ BAD: Cascading effects
useEffect(() => {
  setCards(initialCards);
}, []);

useEffect(() => {
  setGoldCardCount(cards.filter(c => c.gold).length);
}, [cards]);

// ✅ GOOD: Calculate in event handler or during render
function handleClick() {
  const nextCards = initialCards;
  setCards(nextCards);
  setGoldCardCount(nextCards.filter(c => c.gold).length);
}

// ✅ EVEN BETTER: Derive state
const goldCardCount = cards.filter(c => c.gold).length;
```

### 6. Passing Data to Parent
**DON'T** use effects to notify parent components.

```javascript
// ❌ BAD: Effect to notify parent
useEffect(() => {
  onChange(selectedItem);
}, [selectedItem]);

// ✅ GOOD: Call parent function directly in event handler
function handleSelect(item) {
  setSelectedItem(item);
  onChange(item);
}
```

## ✅ When TO Use useEffect

Use effects ONLY for synchronizing with external systems:

1. **Fetching data** (though prefer libraries like React Query/TanStack Query)
2. **Setting up subscriptions** (WebSocket, event listeners)
3. **Triggering animations** via DOM APIs
4. **Analytics/logging** (side effects that don't affect UI)
5. **Browser API integrations** (localStorage, IntersectionObserver, etc.)

```javascript
// ✅ GOOD: Subscribing to external system
useEffect(() => {
  const connection = createConnection();
  connection.connect();
  return () => connection.disconnect();
}, []);
```

## Decision Tree

**Ask yourself**: "Why does this code need to run?"

- **"Because we're displaying this component"** → Calculate during render
- **"Because the user clicked a button"** → Event handler
- **"Because the user navigated to this page"** → Fetch in event handler or use React Query
- **"To keep data in sync with an external system"** → useEffect ✓

## Quick Reference

| Scenario | Use |
|----------|-----|
| Derive state from props/state | Calculate during render |
| Expensive calculation | `useMemo` |
| Reset state on prop change | `key` prop |
| Handle user interaction | Event handler |
| Fetch data | React Query/TanStack Query |
| Subscribe to external system | `useEffect` |

## Common Mistakes

1. **"When something can be calculated from existing props or state, don't put it in state"**
2. **Don't create state variables just to trigger effects** - handle logic directly
3. **Don't synchronize component state with effects** - lift state up instead
4. **Don't use effects for application logic** - use event handlers

---

**Remember**: If you're unsure whether code should be in an effect, it probably shouldn't be. Default to calculating during render or handling in event handlers.
