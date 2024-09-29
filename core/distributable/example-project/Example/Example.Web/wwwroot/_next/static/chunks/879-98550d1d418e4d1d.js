"use strict";(self.webpackChunk_N_E=self.webpackChunk_N_E||[]).push([[879],{9879:function(e,a,t){t.r(a),t.d(a,{DataTableContext:function(){return et},default:function(){return eo},useDataTableContext:function(){return el}});var l=t(4246),s=t(5381),o=t(7378),r=t(7302),d=t(5812),i=t(8176),n=t(9478),p=t(60);let c={page:1,fields:null,orderBy:null,searchField:null,searchValue:null,setData:null,active:null};var u=t(7152),m=t(3529),f=t(2268),v=()=>{var e,a,t;let s=el(),[r,d]=(0,o.useState)({searchField:null,searchValue:null}),[i]=(0,f.c)(r.searchValue,500),n=()=>s.getFields().filter(e=>"string"==typeof e.displayName).map((e,a)=>{var t,l,s,o;let r=null!==(o=null===(t=e.metadataField)||void 0===t?void 0:t.lookupName)&&void 0!==o?o:null===(l=e.metadataField)||void 0===l?void 0:l.name;return{value:"".concat(e.alias).concat(""!==e.alias?".":"").concat(r),label:null===(s=e.metadataField)||void 0===s?void 0:s.displayName}}),p=async()=>{let e=await s.getData({...c,page:1,searchField:r.searchField,searchValue:i,active:"Active"===s.state.activeSwitch,orderBy:s.state.data.orderBy});s.dispatch({type:"SEARCH_VALUE_CHANGE",payload:{searchValue:i,searchField:r.searchField,data:e.data}})};return(0,o.useEffect)(()=>{null!=i&&p()},[i,r.searchField]),(0,o.useEffect)(()=>{if(null!=s.props.tableName&&""!==s.props.tableName){var e,a,t;d({searchField:null!==(t=null===(a=n())||void 0===a?void 0:null===(e=a[0])||void 0===e?void 0:e.value)&&void 0!==t?t:null,searchValue:null})}},[s.props.tableName]),(0,l.jsxs)("div",{className:"flex",children:[(0,l.jsx)(u.Ph,{placeholder:"Search",style:{width:"auto"},data:null!==(e=n())&&void 0!==e?e:[],value:null!==(a=r.searchField)&&void 0!==a?a:"",onChange:e=>{d({...r,searchField:e})}}),(0,l.jsx)(m.o,{placeholder:"Search...",style:{width:"auto"},value:null!==(t=r.searchValue)&&void 0!==t?t:"",onChange:e=>{d({...r,searchValue:e.currentTarget.value})}})]})},h=t(5371),x=t(8255),N=t(823),y=e=>{let a=(0,r.rZ)(),t=el(),{colorScheme:s,toggleColorScheme:o}=(0,N.X)(),d="dark"===s?a.colors.gray[4]:a.colors.dark[7],i="",n="";return null!=t.state.data.orderBy&&t.state.data.orderBy.length>0&&(i=t.state.data.orderBy[0].field,n=t.state.data.orderBy[0].order),(0,l.jsx)(l.Fragment,{children:t.getFields().map((e,a)=>{if(null==e.metadataField)return(0,l.jsx)("th",{style:{width:e.width},className:"".concat(e.width.endsWith("px")?"flex-none":""," truncate"),children:(0,l.jsx)("div",{className:"flex items-center",children:"string"==typeof e.displayName?e.displayName:e.displayName(t.refHandle)})},a);let s=e.metadataField,o=(null==s?void 0:s.lookupName)!=null?s.lookupName:s.name,r="".concat(e.alias).concat(""!==e.alias?".":"").concat(o);if(null!=s)return(0,l.jsx)("th",{style:{width:e.width},className:"".concat(e.width.endsWith("px")?"flex-none":""," truncate"),children:(0,l.jsxs)("div",{className:"flex items-center",children:[(0,l.jsx)("span",{className:"cursor-pointer",onClick:()=>{t.sort(r)},children:s.displayName}),i===r&&"desc"===n&&(0,l.jsx)(h.Z,{size:16,strokeWidth:2,color:d,className:" ml-2"}),i===r&&"asc"===n&&(0,l.jsx)(x.Z,{size:16,strokeWidth:2,color:d,className:"ml-2"})]})},r)})})},g=t(7628),I=e=>{let a=el();return void 0!==e.disabledMessage?(0,l.jsx)("div",{className:"w-full h-full flex flex-col justify-center items-center",children:e.disabledMessage}):e.isLoadingData?(0,l.jsx)("div",{className:"w-full h-full flex justify-center items-center",children:(0,l.jsx)(g.a,{})}):"MISSING_READ_PERMISSIONS"===a.state.type?(0,l.jsx)("div",{className:"w-full h-full flex flex-col justify-center items-center",children:(0,l.jsxs)("div",{children:["Missing read permission for ",e.tableName,"."]})}):void 0},b=t(8117),E=t(226),j=t(1198),w=t(633),O=t(3792),T=t(8076),D=t(9572),A=t(2760),S=t(7577),_=t(8676),C=t(1331),F=e=>{let a=(0,s.Z)(),t=el(),[r,d]=o.useState(null),[i,n]=o.useState(!1),[p,c]=o.useState("create,update"),[m,f]=o.useState([]),v=async()=>{if(null===r)return;n(!0);let l=new FormData;l.append("file",r),l.append("name","TABLE_ImportData"),l.append("parameters",JSON.stringify({tableName:t.props.tableName,operation:p}));let s=await a.execute({url:C.mH,method:"POST",body:l,hideFailureMessage:!0});if((null==s?void 0:s.succeeded)===!1){let e=s.data;if(null!=e&&null!=e.errors&&e.errors.length>0)f(e.errors);else{var o;f([null!==(o=s.friendlyMessage)&&void 0!==o?o:"Unknown error"])}}else(null==s?void 0:s.succeeded)===!0&&(t.refresh(),e.close());n(!1)};return(0,o.useEffect)(()=>{!0===e.opened&&(n(!1),d(null),f([]))},[e.opened]),(0,l.jsx)(S.u,{opened:e.opened,onClose:e.close,title:"Import Data",size:"lg",closeOnEscape:!i,closeOnClickOutside:!i,withCloseButton:!i,centered:!0,styles:{overlay:{zIndex:4e3},inner:{zIndex:4001}},children:m.length>0?(0,l.jsx)("div",{className:"w-full flex flex-col gap-4 h-96",children:(0,l.jsx)("div",{className:"w-full h-full flex flex-col gap-2 overflow-y-auto",children:m.map((e,a)=>(0,l.jsx)("div",{className:"w-full bg-red-100 py-2 rounded-md",children:e},a))})}):(0,l.jsx)(l.Fragment,{children:(0,l.jsxs)("div",{className:"w-full relative",children:[!0===i&&(0,l.jsx)("div",{className:"absolute w-full h-full flex justify-center items-center",children:(0,l.jsx)(g.a,{})}),(0,l.jsxs)("div",{className:"w-full flex flex-col gap-4 ".concat(i?"invisible":""),children:[(0,l.jsx)(u.Ph,{label:"Operation",placeholder:"Select Operation",data:[{value:"create,update",label:"Create and Update"},{value:"create",label:"Create Only"},{value:"update",label:"Update Only"}],value:p,onChange:e=>{c(e)}}),(0,l.jsx)(_.S,{placeholder:"Click to choose import file",label:"File Upload",required:!0,value:r,onChange:e=>{d(e)}}),(0,l.jsx)("div",{className:"w-full flex justify-end",children:(0,l.jsx)(E.z,{onClick:v,disabled:null===r,children:"Upload"})})]})]})})})},L=t(1879),R=()=>{let[e,a]=(0,o.useState)(!1),[t,r]=(0,o.useState)(void 0),d=(0,D.Z)().getIconColor(),i=(0,s.Z)(),n=el(),p=(0,A.ZP)(),[c,u]=(0,L.q)(!1),m=async()=>{let e=await p.getPermissions(i,["TABLE_".concat(n.props.tableName,"_IMPORT"),"TABLE_".concat(n.props.tableName,"_EXPORT"),"ACTION_TABLE_ImportData","ACTION_TABLE_ExportData"]),t=await p.getTablePermissions(i,n.props.tableName),l=!1,s=!1;e.find(e=>e==="TABLE_".concat(n.props.tableName,"_IMPORT"))&&e.find(e=>"ACTION_TABLE_ImportData"===e)&&"NONE"!==t.create&&"NONE"!==t.update&&(null==n.props.canImport||n.props.canImport)&&(l=!0),e.find(e=>e==="TABLE_".concat(n.props.tableName,"_EXPORT"))&&e.find(e=>"ACTION_TABLE_ExportData"===e)&&"NONE"!==t.read&&(s=!0),r({Import:l,Export:s}),null==n.props.disabledMessage&&(l||s)&&a(!0)},f=async()=>{await i.action("TABLE_ExportTemplate",{tableName:n.props.tableName},"ExportTemplate_".concat(n.props.tableName,".xlsx"))},v=async()=>{await i.action("TABLE_ExportData",{query:{tableName:n.props.tableName,fields:["*"],orderBy:n.props.orderBy,filters:n.props.filters,joins:n.props.joins,except:n.props.except,maxResults:999999}},"ExportData_".concat(n.props.tableName,".xlsx"))};return((0,o.useEffect)(()=>{t||m()},[t,n.state.isLoadingData]),(0,o.useEffect)(()=>{null==n.props.disabledMessage&&((null==t?void 0:t.Import)||(null==t?void 0:t.Export))&&a(!0)},[n.props.disabledMessage]),e&&null!=n.state.metadata)?(0,l.jsxs)(l.Fragment,{children:[(0,l.jsx)(F,{opened:c,close:u.close}),(0,l.jsxs)(b.v,{shadow:"md",width:200,children:[(0,l.jsx)(b.v.Target,{children:(0,l.jsx)(E.z,{radius:"xl",variant:"subtle",styles:{root:{padding:6}},children:(0,l.jsx)(j.Z,{size:24,strokeWidth:2,color:d})})}),(0,l.jsxs)(b.v.Dropdown,{children:[((null==t?void 0:t.Import)||(null==t?void 0:t.Export))&&(0,l.jsx)(b.v.Label,{children:"Data"}),(null==t?void 0:t.Import)&&(0,l.jsxs)(l.Fragment,{children:[(0,l.jsx)(b.v.Item,{icon:(0,l.jsx)(w.Z,{size:14}),onClick:f,children:"Export Template"}),(0,l.jsx)(b.v.Item,{icon:(0,l.jsx)(O.Z,{size:14}),onClick:u.open,children:"Import Data"})]}),(null==t?void 0:t.Export)&&(0,l.jsx)(l.Fragment,{children:(0,l.jsx)(b.v.Item,{icon:(0,l.jsx)(T.Z,{size:14}),onClick:v,children:"Export Data"})})]})]})]}):(0,l.jsx)(l.Fragment,{})},B=e=>{var a,t,s,o,u,m,f,h,x,N,g;let b=el(),E=(0,r.rZ)(),j=void 0!==b.state.metadata,w=!0===b.props.scrollable||void 0===b.props.scrollable,O=(void 0===b.props.canCreate||!0===b.props.canCreate)&&"NONE"!==b.state.permissions.create&&void 0===b.props.disabledMessage,T=null==b.props.disabledMessage&&"NONE"!==b.state.permissions.read,D=null!=b.props.customCreateButton?null===(a=(t=b.props).customCreateButton)||void 0===a?void 0:a.call(t,()=>b.openForm(void 0)):void 0,A="NONE"!==b.state.permissions.read&&void 0===b.props.disabledMessage&&(null==b.props.searchable||!0===b.props.searchable),S=!0===b.props.showActiveSwitch&&"NONE"!==b.state.permissions.read&&void 0===b.props.disabledMessage,_=!0===b.props.showOptions||null==b.props.showOptions,C=void 0!==b.props.disabledMessage||"NONE"===b.state.permissions.read||!0===b.state.isLoadingData?(0,l.jsx)(I,{tableName:b.props.tableName,permissions:b.state.permissions,isLoadingData:b.state.isLoadingData,disabledMessage:b.props.disabledMessage}):void 0,F=async e=>{await b.getData({...c,page:e,orderBy:b.state.data.orderBy,setData:!0}),void 0!==b.props.onPageChange&&b.props.onPageChange(e)},L=async e=>{let a=await b.getData({...c,page:1,active:"Active"===e,setData:!0,searchField:b.state.searchField,searchValue:b.state.searchValue,orderBy:b.state.data.orderBy});b.dispatch({type:"ACTIVE_SWITCH_CHANGE",payload:{activeSwitch:e,data:a.data}})};return(0,l.jsxs)("div",{className:"relative flex flex-col h-full w-full rtable",children:[void 0!==C&&(0,l.jsx)("div",{className:" absolute w-full h-full pt-11",children:(0,l.jsx)("div",{className:"w-full h-full",children:C})}),(0,l.jsxs)("div",{className:"text-lg flex justify-between items-center",children:[(0,l.jsx)("div",{className:"flex items-center",children:j?void 0!==b.props.title?b.props.title:"Manage ".concat(void 0===b.state.metadata?b.props.tableName:null===(s=b.state.metadata)||void 0===s?void 0:s.displayName,"s"):""}),(0,l.jsxs)("div",{className:"flex items-center gap-2",children:[_&&(0,l.jsx)(R,{}),S&&(0,l.jsx)("div",{className:" m-2",children:(0,l.jsx)(d.s,{data:["Active","Inactive"],size:"sm",styles:{label:{paddingLeft:"0.5125rem",paddingRight:"0.5125rem",paddingTop:0,paddingBottom:0}},onChange:e=>{L(e)}})}),!0===A?(0,l.jsx)(v,{}):(0,l.jsx)("div",{className:" w-1 h-9"}),O&&null==D&&(0,l.jsx)("div",{onClick:()=>{b.openForm(void 0),null!=b.props.formOnOpen&&b.props.formOnOpen("CREATE",void 0)},className:"p-1 rounded-full w-8 h-8 flex justify-center items-center cursor-pointer",style:{backgroundColor:E.fn.primaryColor()},children:(0,l.jsx)(p.Z,{size:22,strokeWidth:2,color:"white"})}),O&&null!=D&&D]})]}),(0,l.jsxs)(i.i,{highlightOnHover:null===(h=null===(o=b.props.tableStyle)||void 0===o?void 0:o.highlighOnHover)||void 0===h||h,striped:null!==(x=null===(u=b.props.tableStyle)||void 0===u?void 0:u.striped)&&void 0!==x&&x,withBorder:null!==(N=null===(m=b.props.tableStyle)||void 0===m?void 0:m.withBorder)&&void 0!==N&&N,withColumnBorders:null!==(g=null===(f=b.props.tableStyle)||void 0===f?void 0:f.withColumnBorders)&&void 0!==g&&g,className:"".concat(w?"  overflow-x-hidden":""," flex flex-col grow"),children:[(0,l.jsx)("thead",{className:"".concat(w?"px-2":""),children:(0,l.jsx)("tr",{className:"flex",children:(0,l.jsx)(y,{})})}),(0,l.jsx)("tbody",{className:"relative overflow-x-hidden ".concat(w?"grow overflow-y-scroll pl-2 pr-0.5":""," "),children:e.children})]}),!0===T&&(0,l.jsxs)("div",{className:"w-full flex justify-between items-center mt-4 px-2",children:[(0,l.jsx)("div",{className:" text-sm",children:void 0!==b.state.data.totalResults&&b.state.data.maxResults&&b.state.data.currentPage&&(0,l.jsxs)(l.Fragment,{children:[0===b.state.data.results.length?0:b.state.data.currentPage*b.state.data.maxResults-(b.state.data.maxResults-1)," - ",b.state.data.currentPage*b.state.data.maxResults>b.state.data.totalResults?b.state.data.totalResults:b.state.data.currentPage*b.state.data.maxResults," of ",b.state.data.totalResults]})}),(null==b.props.pagination||!0===b.props.pagination)&&(0,l.jsx)(n.t,{value:b.state.data.currentPage,total:b.state.data.pages<1?1:b.state.data.pages,onChange:e=>{F(e)},size:"sm",withEdges:!0})]})]})},M=t(2289),P=t(2063),k=t(625),Z=t(2815),W=t(9803);let V=(0,o.forwardRef)((e,a)=>{var t,s,r,d;let i=el(),n=(0,k.Z)({tableName:null!==(s=null===(t=i.state.metadata)||void 0===t?void 0:t.tableName)&&void 0!==s?s:"",id:i.state.editRecordId,metadata:i.state.metadata,defaults:i.props.formFieldDefaults,lookupExclusions:i.props.formLookupExclusions,lookupQueries:i.props.formLookupQueries,canCreate:"NONE"!==i.state.permissions.create,canUpdate:i.props.canUpdate,onPreSave:async e=>(!0===i.props.formCloseOnCreate||i.props.formCloseOnCreate,null!=i.props.formOnPreSave&&i.props.formOnPreSave(e),{continue:!0}),onPostSave:async(e,a,t)=>{if("FAILED"===e);else if("CREATE"===e&&!1===i.props.formCloseOnCreate&&n.setSnapshot(t),"UPDATE"===e&&!1===i.props.formCloseOnUpdate&&n.setSnapshot(t),null!=i.props.formOnPostSave&&i.props.formOnPostSave(e,n.data),"CREATE"===e&&(!0===i.props.formCloseOnCreate||void 0===i.props.formCloseOnCreate)||"UPDATE"===e&&(!0===i.props.formCloseOnUpdate||void 0===i.props.formCloseOnUpdate)){var l,s;i.getData({...c,page:null===(l=i.state.data)||void 0===l?void 0:l.currentPage,orderBy:null===(s=i.state.data)||void 0===s?void 0:s.orderBy,setData:!0})}}}),p=()=>{if(void 0!==i.state.metadata){let e=i.state.metadata.fields.filter(e=>void 0===i.props.formFields||i.props.formFields.includes(e.name));if(e.length<=3)return 12/e.length}return 4};return(0,o.useImperativeHandle)(a,()=>({formBuilder:n})),(0,l.jsxs)(l.Fragment,{children:[(0,l.jsx)(S.u,{opened:"START_INITIAL_LOAD"===n.stateType,onClose:()=>{},withCloseButton:!1,size:"auto",closeOnClickOutside:!1,closeOnEscape:!1,overlayProps:{blur:3},styles:{overlay:{...void 0!==i.props.formZIndex&&{zIndex:i.props.formZIndex}},inner:{...void 0!==i.props.formZIndex&&{zIndex:i.props.formZIndex+1}},content:{display:"none"}},centered:!0}),(0,l.jsx)(S.u,{title:null!=i.props.formTitle?i.props.formTitle:"".concat("UPDATE"===n.operation?"Edit":"Create"," ").concat(void 0===i.state.metadata?i.props.tableName:null===(d=i.state.metadata)||void 0===d?void 0:d.displayName),opened:i.formDisclosure.opened&&null!=n.metadata&&"START_INITIAL_LOAD"!==n.stateType,onClose:()=>{if(null!=i.props.formOnClose&&i.props.formOnClose(),n.isSubmitted){var e,a;i.getData({...c,page:null===(e=i.state.data)||void 0===e?void 0:e.currentPage,orderBy:null===(a=i.state.data)||void 0===a?void 0:a.orderBy,setData:!0})}else i.formDisclosure.close()},transitionProps:{transition:"fade",duration:100},closeOnClickOutside:!1,closeOnEscape:null===(r=i.props.formCloseOnEscape)||void 0===r||r,size:(()=>{if(null!=i.props.formMaxWidth)return"".concat(i.props.formMaxWidth,"rem");if(void 0!==i.state.metadata){let e=i.state.metadata.fields.filter(e=>void 0===i.props.formFields||i.props.formFields.includes(e.name));if(e.length<=3){let a=e.length/3*72;return void 0!==i.props.formMaxWidth&&a<i.props.formMaxWidth?"".concat(i.props.formMaxWidth,"rem"):"".concat(a,"rem")}}return void 0!==i.props.formMaxWidth&&i.props.formMaxWidth>72?"".concat(i.props.formMaxWidth,"rem"):"72rem"})(),overlayProps:{blur:3},styles:{root:{overflow:"visible"},body:{...!0===n.isLoading&&{position:"relative",overflow:"hidden",paddingLeft:0,paddingRight:0}},overlay:{...void 0!==i.props.formZIndex&&{zIndex:i.props.formZIndex}},inner:{...void 0!==i.props.formZIndex&&{zIndex:i.props.formZIndex+1}}},centered:!0,children:(0,l.jsxs)(W.Z,{formBuilder:n,children:[void 0!==i.props.customForm?i.props.customForm(n,i.formDisclosure):void 0!==i.state.metadata&&(void 0!==i.props.formFields?i.props.formFields:i.state.metadata.fields).map((e,a)=>{if(a%3==0){var t,s;return(0,l.jsx)(M.r,{children:void 0!==i.state.metadata&&(null===(t=void 0!==i.props.formFields?i.props.formFields:null===(s=i.state.metadata)||void 0===s?void 0:s.fields.map(e=>e.name))||void 0===t?void 0:t.slice(a,a+3).map((e,t)=>{var s;let o=null===(s=i.state.metadata)||void 0===s?void 0:s.fields.find(a=>a.name===e);return void 0===o?("function"!=typeof e&&"object"!=typeof e&&console.warn("Couldn't find field named ".concat(e,".")),(0,l.jsx)("div",{className:"hidden"},"".concat(a).concat(t))):("function"==typeof e&&"".concat(a).concat(t),(0,l.jsx)(M.r.Col,{span:p(),children:(0,l.jsx)(P.Z,{focus:0===a&&0===t,name:o.name})},"".concat(o.name).concat(a).concat(t,"}")))}))},a)}return(0,l.jsx)("div",{className:"hidden"},a)}),void 0!==i.props.appendCustomForm?(0,l.jsx)("div",{className:" mt-2",children:i.props.appendCustomForm(n)}):(0,l.jsx)(l.Fragment,{}),null==i.props.customForm&&(0,l.jsxs)("div",{className:"w-full flex justify-end",children:[null!=i.props.formAppendButton?(0,l.jsx)("div",{className:"mr-2 mt-4",children:i.props.formAppendButton(n)}):(0,l.jsx)(l.Fragment,{}),!0!==i.props.formHideSaveButton&&(void 0!==n.snapshot&&!0===n.canUpdate||void 0===n.snapshot&&!0===n.canCreate)?(0,l.jsx)("div",{className:"mt-4",children:(0,l.jsx)(Z.Z,{})}):(0,l.jsx)(l.Fragment,{})]})]})})]})});V.displayName="DataForm";let z={type:"START_INITIAL_LOAD",id:"",isLoadingData:!0,permissions:{read:"NONE",create:"NONE",delete:"NONE",update:"NONE"},isTableNotFound:!1,metadata:void 0,joinMetadata:[],data:{pages:0,currentPage:1,totalResults:0,orderBy:[],tableName:"",maxResults:10,results:[]},searchField:"",searchValue:"",activeSwitch:null,editRecordId:null},H=(e,a)=>{switch(console.log("DataTable: "+a.type),a.type){case"START_INITIAL_LOAD":return{...z,isLoadingData:!0,type:"START_INITIAL_LOAD"};case"MISSING_READ_PERMISSIONS":return{...z,isLoadingData:!1,permissions:{...e.permissions,read:"NONE"},type:"MISSING_READ_PERMISSIONS"};case"TABLE_NOT_FOUND":return{...z,isLoadingData:!1,isTableNotFound:!0,type:"TABLE_NOT_FOUND"};case"INITIAL_LOAD_COMPLETE":return{...z,...a.payload,editRecordId:e.editRecordId,type:e.editRecordId?"OPEN_FORM":"INITIAL_LOAD_COMPLETE"};case"SET_IS_LOADING":return{...e,isLoadingData:!0,type:"SET_IS_LOADING"};case"SET_DONE_LOADING":return{...e,isLoadingData:!1,data:a.payload,type:"SET_DONE_LOADING"};case"SEARCH_VALUE_CHANGE":return{...e,searchValue:a.payload.searchValue,searchField:a.payload.searchField,data:a.payload.data,type:"SEARCH_VALUE_CHANGE"};case"ACTIVE_SWITCH_CHANGE":return{...e,activeSwitch:a.payload.activeSwitch,type:"ACTIVE_SWITCH_CHANGE"};case"SET_METADATA":return{...e,metadata:a.payload.metadata,joinMetadata:a.payload.joinMetadata,type:"SET_METADATA"};case"SET_DATA":return{...e,data:{...e.data,results:a.payload.setDataFunction(e.data.results)},type:"SET_DATA"};case"OPEN_FORM":var t;return{...e,editRecordId:a.payload.editRecordId,formTableName:null===(t=e.metadata)||void 0===t?void 0:t.tableName,type:"OPEN_FORM"};case"CLOSE_FORM":return{...e,editRecordId:void 0,formTableName:void 0,type:"CLOSE_FORM"}}return{...e,...a.payload}};var U=t(496),G=t(5279),q=t(3660),X=t(4611),Q=t(7693),Y=t.n(Q),J=e=>{var a,t,d,i;let n;let p=(0,s.Z)(),c=(0,o.useContext)(U.Il),u=el(),m=(0,r.rZ)(),{colorScheme:f,toggleColorScheme:v}=(0,N.X)(),h=null!=e.record.IsActive&&!0===e.record.IsActive,x="dark"===f?m.colors[m.primaryColor][m.fn.primaryShade()-3]:m.colors[m.primaryColor][m.fn.primaryShade()],y=async e=>{let a=e[u.props.tableName+"Id"];if("Delete"===u.props.deleteBehavior||null==u.props.deleteBehavior){if(void 0===u.props.confirmDelete||!0===u.props.confirmDelete){let s="Confirm",o="Are you sure you want to delete this record?",r=!0;if(null!=u.props.deleteConfirmation){var t,l;let a=await u.props.deleteConfirmation(e);s=null!==(t=a.title)&&void 0!==t?t:s,o=null!==(l=a.message)&&void 0!==l?l:o,r=void 0===a.showPrompt||a.showPrompt}r?null==c||c.showConfirm(o,async()=>{await I(e,u.props.tableName,a)},()=>{},s):await I(e,u.props.tableName,a)}else await I(e,u.props.tableName,a)}"Deactivate"===u.props.deleteBehavior&&g(u.props.tableName,a,!h)},g=async(e,a,t)=>{u.dispatch({type:"SET_IS_LOADING"}),await p.update(e,{["".concat(u.props.tableName,"Id")]:a,IsActive:t}),await u.refresh()},I=async(e,a,t)=>{u.dispatch({type:"SET_IS_LOADING"}),await p.delete(a,t),null!=u.props.onPostDelete&&u.props.onPostDelete(e),await u.refresh()},b="";if(null!=e.fieldInfo.metadataField){let a="".concat(e.fieldInfo.alias).concat(""!==e.fieldInfo.alias?".":"").concat(e.fieldInfo.metadataField.name);void 0!==e.record[e.fieldInfo.metadataField.lookupName]?b=e.record[e.fieldInfo.metadataField.lookupName]:void 0!==e.record[a]&&(b=((a,t)=>{if("Boolean"===a)return t?"Yes":"No";if("DateTime"===a){var l;if(null==t||"0001-01-01T00:00:00"===t)return"";if((null===(l=e.fieldInfo.metadataField)||void 0===l?void 0:l.dateFormat)!=null)return Y()(Date.parse(t)).format(e.fieldInfo.metadataField.dateFormat);let a=new Date(t.replace("Z","")),s=String(a.getMonth()+1).padStart(2,"0"),o=String(a.getDate()).padStart(2,"0"),r=a.getFullYear();return"".concat(s,"/").concat(o,"/").concat(r)}return t})(e.fieldInfo.metadataField.type,e.record[a]))}return(0,l.jsxs)("td",{className:"".concat((null===(a=u.props.tableStyle)||void 0===a?void 0:a.truncate)===!0||(null===(t=u.props.tableStyle)||void 0===t?void 0:t.truncate)==null?"truncate":""," cursor-pointer relative ").concat(e.fieldInfo.width.endsWith("px")?"flex-none":""),style:{width:e.fieldInfo.width,height:(null===(d=u.props.tableStyle)||void 0===d?void 0:d.truncate)===!0||(null===(i=u.props.tableStyle)||void 0===i?void 0:i.truncate)==null?"36.7px":""},children:[null!=e.fieldInfo.body?e.fieldInfo.body(e.record,u.refHandle):b,!0===e.record._ui_info_.canDelete&&(void 0===u.props.canDelete||!0===u.props.canDelete)&&(n=e.record,(0,l.jsx)("div",{onClick:e=>{e.stopPropagation(),y(n)},className:"absolute right-0 top-0 bottom-0 flex items-center cursor-pointer xams_delete",children:(0,l.jsx)("div",{className:"flex items-center p-0.5 rounded",style:{backgroundColor:"dark"===f?m.colors.gray[m.fn.primaryShade()+2]:m.colors.gray[m.fn.primaryShade()-5]},children:null==u.props.deleteBehavior||"Delete"===u.props.deleteBehavior?(0,l.jsx)(G.Z,{size:26,strokeWidth:2,color:x}):h?(0,l.jsx)(q.Z,{size:26,strokeWidth:2,color:x}):(0,l.jsx)(X.Z,{size:26,strokeWidth:2,color:x})})}))]})},K=()=>{var e;let a=el();return(0,l.jsx)(l.Fragment,{children:!1===a.state.isLoadingData&&(null===(e=a.state.data)||void 0===e?void 0:e.results)!=null&&a.state.data.results.filter(e=>{var t;return a.props.tableName===(null===(t=a.state.metadata)||void 0===t?void 0:t.tableName)}).map((e,t)=>void 0!==a.props.customRow?(0,l.jsx)("tr",{className:"w-full",children:a.props.customRow(e)},e[a.props.tableName+"Id"]):(0,l.jsx)("tr",{className:"flex relative items-start",onClick:()=>{a.openForm(e),null!=a.props.formOnOpen&&a.props.formOnOpen("UPDATE",e)},children:a.getFields().map((t,s)=>{let o="".concat(e[a.props.tableName+"Id"],"-").concat(s);return(0,l.jsx)(J,{record:e,fieldInfo:t},o)})},e[a.props.tableName+"Id"]))})},$=t(4323),ee=t(1765);function ea(e){return null!==e}let et=o.createContext(null),el=()=>{let e=o.useContext(et);if(null==e)throw Error("useDataTableContext must be used within a DataTableContextProvider");return e},es=(0,o.forwardRef)((e,a)=>{let t=(0,s.Z)(),r=(0,o.useContext)($.q3),d=(0,A.eL)(),i=(0,ee.Z)(),n=(0,o.useRef)(null),[p,u]=(0,o.useState)(i.get()),[m,f]=(0,o.useReducer)(H,{...z,id:p}),v=(0,o.useRef)(m);if(null!=e.fields&&null!=e.columnWidths&&e.fields.length!==e.columnWidths.length)throw Error("Fields and columnWidths must be the same length in DataTable");let h=async()=>{var a;f({type:"START_INITIAL_LOAD"});let l=null!=m.metadata&&m.metadata.tableName===e.tableName?m.metadata:await t.metadata(e.tableName),s=[];if(null!=e.joins)for(let a of e.joins){let e=await t.metadata(a.toTable);null!=e&&s.push(e)}if(null==l){f({type:"TABLE_NOT_FOUND"});return}if(e.disabledMessage){f({type:"SET_METADATA",payload:{metadata:l,joinMetadata:s}});return}let o=await d.getTablePermissions(t,e.tableName);if("NONE"===o.read){f({type:"MISSING_READ_PERMISSIONS"});return}let r=null!==(a=e.fields)&&void 0!==a?a:l.fields.map(e=>e.name).slice(0,7),i=await N({...c,metadata:l,joinMetadata:s,fields:r,page:1,active:!0===e.showActiveSwitch||null,orderBy:e.orderBy,searchField:"",searchValue:""});(null==i?void 0:i.succeeded)&&(x(l),null!=e.onInitialLoad&&e.onInitialLoad(i.data.results),f({type:"INITIAL_LOAD_COMPLETE",payload:{permissions:o,metadata:l,joinMetadata:s,data:i.data,isLoadingData:!1,activeSwitch:!0===e.showActiveSwitch?"Active":null}}))},x=a=>{void 0!==e.fields&&e.fields.forEach(t=>{"function"!=typeof t&&(void 0!==a.fields.find(e=>e.name===t)||t===e.tableName+"Id"||"object"==typeof t||t.includes(".")||console.warn("Field ".concat(t," not found in metadata for table ").concat(e.tableName)))})},N=async a=>{var l,s,o,r,d,i,n,p,c,u,v,h,x,N;(null==a?void 0:a.setData)!=null&&!0===a.setData&&!1===m.isLoadingData&&(null==a.showLoading||!0===a.showLoading)&&f({type:"SET_IS_LOADING"});let y=null!==(d=null!==(r=null==a?void 0:a.metadata)&&void 0!==r?r:m.metadata)&&void 0!==d?d:await t.metadata(e.tableName),g=null!==(n=null!==(i=null===(l=e.fields)||void 0===l?void 0:l.filter(e=>"string"==typeof e&&!e.includes(".")))&&void 0!==i?i:null==a?void 0:null===(s=a.fields)||void 0===s?void 0:s.filter(e=>"string"==typeof e&&!e.includes(".")))&&void 0!==n?n:null==y?void 0:y.fields.map(e=>e.name).slice(0,7),I=[];if(null==g||g.forEach(e=>{if(e.endsWith("Id")){let a=null==y?void 0:y.fields.find(a=>a.name===e);(null==a?void 0:a.type)==="Lookup"&&void 0===g.find(e=>e===a.lookupName)&&I.push(a.lookupName)}}),null==g||g.push(...I),null!=e.additionalFields)for(let a of e.additionalFields)void 0===g.find(e=>e===a)&&g.push(a);(null==g?void 0:g.find(a=>a===e.tableName+"Id"))===void 0&&(null==g||g.push(e.tableName+"Id")),"Deactivate"===e.deleteBehavior&&void 0===g.find(e=>"IsActive"===e)&&g.push("IsActive");let b=await t.read({tableName:e.tableName,page:null==a?1:a.page,fields:g,maxResults:null!==(p=e.maxResults)&&void 0!==p?p:10,except:e.except,orderBy:null!==(u=null!==(c=null==a?void 0:a.orderBy)&&void 0!==c?c:null===(o=m.data)||void 0===o?void 0:o.orderBy)&&void 0!==u?u:[],filters:[{field:null==a?"":null!==(h=null!==(v=a.searchField)&&void 0!==v?v:m.searchField)&&void 0!==h?h:"",value:null==a?"":null!==(N=null!==(x=a.searchValue)&&void 0!==x?x:m.searchValue)&&void 0!==N?N:""},...!0===e.showActiveSwitch?[{field:"IsActive",operator:"==",value:(null==a?void 0:a.active)!=null?null==a?void 0:a.active.toString():"true"}]:[],...void 0!==e.filters?e.filters.map(e=>({field:e.field,operator:e.operator,value:e.value})):[]],joins:void 0!==e.joins?e.joins:[]});return(null==a?void 0:a.setData)!=null&&!0===a.setData&&f({type:"SET_DONE_LOADING",payload:b.data}),null!=e.onDataLoaded&&e.onDataLoaded(b.data),b},y=async a=>{if(null==e.disabledMessage){var t,l;let s=null===(t=m.data)||void 0===t?void 0:t.orderBy;(null==s||0===s.length)&&(s=null!==(l=e.orderBy)&&void 0!==l?l:[]),await N({...c,page:v.current.data.currentPage,showLoading:a,searchField:v.current.searchField,searchValue:v.current.searchValue,orderBy:s,active:"Active"===v.current.activeSwitch,setData:!0})}},g=a=>null!=e.columnWidths&&e.columnWidths.length>=a?e.columnWidths[a]:"100%",I=function(a){let t=!(arguments.length>1)||void 0===arguments[1]||arguments[1];(void 0===e.onRowClick||!0!==t||!1!==e.onRowClick(a))&&f({type:"OPEN_FORM",payload:{editRecordId:null!=a?a[e.tableName+"Id"]:null}})},b=async e=>{null!=m.data.orderBy&&m.data.orderBy.length>0&&m.data.orderBy[0].field===e&&"asc"===m.data.orderBy[0].order?N({...c,page:m.data.currentPage,orderBy:[{field:e,order:"desc"}],setData:!0}):N({...c,page:m.data.currentPage,orderBy:[{field:e,order:"asc"}],setData:!0})};(0,o.useEffect)(()=>{v.current=m},[m.data]),(0,o.useEffect)(()=>{void 0!==e.tableName&&h()},[e.tableName,e.disabledMessage]),(0,o.useEffect)(()=>{if("OPEN_FORM"!==m.type&&null!=e.refreshInterval){let a=setInterval(async()=>{await y(!1)},e.refreshInterval);return()=>clearInterval(a)}},[e.refreshInterval,m.type]);let E={refresh:y,openForm:e=>I(e,!1),dataTableId:p,getRecords:()=>{var e;return null===(e=m.data)||void 0===e?void 0:e.results},setRecords:e=>{f({type:"SET_DATA",payload:{setDataFunction:e}})},showLoading:()=>{f({type:"SET_IS_LOADING"})},sort:b};return(null!=r&&r.formBuilder.addDataTable(E),(0,o.useImperativeHandle)(a,()=>E),!0===m.isTableNotFound)?(0,l.jsx)("div",{className:"w-full h-full flex justify-center items-center",children:"Table not found."}):(0,l.jsxs)(et.Provider,{value:{props:e,state:m,refHandle:E,dispatch:f,openForm:I,refresh:y,formDisclosure:{opened:"OPEN_FORM"===m.type,close:()=>{f({type:"CLOSE_FORM"})}},getData:N,getFields:()=>{if(null!=e.fields&&null!=m.metadata)return e.fields.map((a,t)=>{if("string"==typeof a){let s=a,o=m.metadata,r="";if(a.includes(".")){var l;let t=a.split(".");r=t[0],s=t[1];let d=null===(l=e.joins)||void 0===l?void 0:l.find(e=>e.alias===r);null!=d&&(o=m.joinMetadata.find(e=>e.tableName===d.toTable))}let d=null==o?void 0:o.fields.find(e=>e.name===s);return null==d?null:{displayName:s,metadataField:d,body:null,width:g(t),alias:r}}return"object"==typeof a?{displayName:a.header,body:a.body,width:g(t),alias:""}:null}).filter(ea);if(null==e.fields&&null!=m.metadata){var a;return null===(a=m.metadata)||void 0===a?void 0:a.fields.map((e,a)=>a<=6?{displayName:e.name,metadataField:e,body:null,width:g(a),alias:""}:null).filter(ea)}return[]},sort:b},children:["OPEN_FORM"===m.type&&(0,l.jsx)(V,{ref:n}),m.data&&(0,l.jsx)(B,{children:(0,l.jsx)(K,{})})]})});es.displayName="DataTable";var eo=es}}]);