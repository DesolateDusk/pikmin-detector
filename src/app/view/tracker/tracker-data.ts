export type PikminColor = 'red' | 'yellow' | 'blue' | 'purple' | 'white' | 'rock' | 'winged' | 'ice';
export type CollectionStatus = 'collected' | 'missing';
export type PageId = 'overview' | 'tracking' | 'nearby';

export interface ColorOption {
  id: PikminColor;
  label: string;
  shortLabel: string;
  hex: string;
}

export interface DecorVariant {
  id: string;
  name: string;
}

export interface DecorSeries {
  id: string;
  name: string;
  location: string;
  icon: string;
  tint: string;
  variants: readonly DecorVariant[];
}

export interface PureSpot {
  id: string;
  name: string;
  area: string;
  seriesId: string;
  distanceKm: number;
  source: string;
  updated: string;
  sourceUrl: string;
}

export interface RecognitionResult {
  id: string;
  seriesId: string;
  variantId: string;
  color: PikminColor;
  capturedAt: string;
}

export const COLORS: readonly ColorOption[] = [
  { id: 'red', label: '紅色', shortLabel: '紅', hex: '#eb6e63' },
  { id: 'yellow', label: '黃色', shortLabel: '黃', hex: '#f2c653' },
  { id: 'blue', label: '藍色', shortLabel: '藍', hex: '#70a9d8' },
  { id: 'purple', label: '紫色', shortLabel: '紫', hex: '#a88dbd' },
  { id: 'white', label: '白色', shortLabel: '白', hex: '#e7e9e5' },
  { id: 'rock', label: '岩石', shortLabel: '岩', hex: '#8e9698' },
  { id: 'winged', label: '羽翅', shortLabel: '羽', hex: '#e5a4bb' },
  { id: 'ice', label: '冰', shortLabel: '冰', hex: '#9bd6db' },
];

export const SERIES: readonly DecorSeries[] = [
  { id: 'roadside', name: '路邊', location: '街道與散步路線', icon: '✦', tint: 'sage', variants: [{ id: 'sticker', name: '貼紙' }, { id: 'coin', name: '硬幣' }] },
  { id: 'cafe', name: '咖啡廳', location: '咖啡館周圍', icon: '☕', tint: 'peach', variants: [{ id: 'cup', name: '咖啡杯' }] },
  { id: 'restaurant', name: '餐廳', location: '餐飲場所附近', icon: '◒', tint: 'coral', variants: [{ id: 'chef', name: '主廚帽' }] },
  { id: 'park', name: '公園', location: '公園與綠地', icon: '❀', tint: 'sage', variants: [{ id: 'clover', name: '四葉草' }] },
  { id: 'pharmacy', name: '藥局', location: '藥局周圍', icon: '✚', tint: 'lilac', variants: [{ id: 'toothbrush', name: '牙刷' }] },
  { id: 'bookstore', name: '書店', location: '書店周圍', icon: '▤', tint: 'sky', variants: [{ id: 'tiny-book', name: '小書' }] },
  { id: 'station', name: '車站', location: '車站周圍', icon: '⌁', tint: 'butter', variants: [{ id: 'ticket', name: '車票' }] },
  { id: 'bakery', name: '麵包店', location: '烘焙坊周圍', icon: '◡', tint: 'peach', variants: [{ id: 'baguette', name: '法國麵包' }] },
];

export const PURE_SPOTS: readonly PureSpot[] = [
  { id: 'spot-0', name: '林蔭步道入口', area: '臺北市・中正區', seriesId: 'roadside', distanceKm: 0.5, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
  { id: 'spot-1', name: '中山堂周邊', area: '臺北市・中正區', seriesId: 'park', distanceKm: 0.8, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
  { id: 'spot-2', name: '城中街角', area: '臺北市・中正區', seriesId: 'cafe', distanceKm: 1.3, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
  { id: 'spot-3', name: '北門散步路線', area: '臺北市・中正區', seriesId: 'bookstore', distanceKm: 1.9, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
  { id: 'spot-4', name: '松江路口', area: '臺北市・中山區', seriesId: 'pharmacy', distanceKm: 2.4, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
  { id: 'spot-5', name: '車站前廣場', area: '臺北市・中正區', seriesId: 'station', distanceKm: 2.8, source: '樹懶生活', updated: '示意資料', sourceUrl: 'https://treelazy.com/pikmin' },
];

export const RECOGNITION_RESULTS: readonly RecognitionResult[] = [
  { id: 'result-1', seriesId: 'cafe', variantId: 'cup', color: 'red', capturedAt: '示範紀錄 01' },
  { id: 'result-2', seriesId: 'park', variantId: 'clover', color: 'winged', capturedAt: '示範紀錄 02' },
];

export function entryId(seriesId: string, variantId: string, color: PikminColor): string {
  return `${seriesId}/${variantId}/${color}`;
}

export const INITIAL_STATUSES: Readonly<Record<string, CollectionStatus>> = {
  [entryId('roadside', 'sticker', 'red')]: 'collected',
  [entryId('roadside', 'sticker', 'yellow')]: 'collected',
  [entryId('roadside', 'sticker', 'blue')]: 'missing',
  [entryId('roadside', 'coin', 'red')]: 'missing',
  [entryId('cafe', 'cup', 'red')]: 'missing',
  [entryId('cafe', 'cup', 'yellow')]: 'collected',
  [entryId('cafe', 'cup', 'blue')]: 'missing',
  [entryId('restaurant', 'chef', 'purple')]: 'collected',
  [entryId('park', 'clover', 'red')]: 'collected',
  [entryId('park', 'clover', 'winged')]: 'missing',
  [entryId('pharmacy', 'toothbrush', 'white')]: 'missing',
  [entryId('bookstore', 'tiny-book', 'ice')]: 'missing',
  [entryId('station', 'ticket', 'rock')]: 'collected',
  [entryId('station', 'ticket', 'blue')]: 'missing',
};
