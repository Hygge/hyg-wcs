<script setup>
import {reactive, ref, onMounted} from 'vue';
import {
  DelOpcUa,
  GetOpcUaList,
  GetS7List,
  OpcExportExcel,
  OpcImportExcel,
  S7Export,
  S7Import
} from "@/api/dataPoint.js";
import {DeleteOutlined} from "@ant-design/icons-vue";
import {message} from "ant-design-vue";
import {useRouter} from "vue-router";
import fileDownload from "js-file-download";

defineOptions({
  name : 'opcUa'
})

onMounted(() => {
  search(null);
})

const ip = ref('')
const name = ref('')
const category = ref('')
const identifier = ref('')

const reset = () => {
  ip.value = ''
  name.value = ''
  category.value = ''
  identifier.value = ''
}
const search = (page) =>{
  let query = {name:name.value, category: category.value, ip: ip.value, identifier: identifier.value,
    pageNum: paginationer.current, pageSize: paginationer.pageSize }
  if (page){
    query.pageNum = page.current;
    query.pageSize = page.pageSize;
  }
  list(query)
}

const list = (query) => {
  GetOpcUaList(query).then(res=>{
    console.log(res.data)
    if (res.code === 200){
      let result = res.data
      dataSource.value = result.rows
      paginationer.total = result.total
      paginationer.current = result.pageNum
      paginationer.pageSize = result.pageSize
    }
  })

}
const paginationer = reactive({
  pageSize: 10,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '30', '40'],
  showQuickJumper: false,
  showTotal: total => `共 ${total} 条`,
  current: 1,
  total: 0,
})

const selectRow = ref([])
const dataSource = ref([ ])
const columns= ref([
  {    title: '名称',    dataIndex: 'name',    key: 'name',   align: 'center' , ellipsis: true,  show:true },
  {    title: '类别',    dataIndex: 'category',    key: 'category',    align: 'center',  show:true  },
  {    title: 'endpoint',    dataIndex: 'endpoint',    key: 'endpoint',    align: 'center',  show:true  },
  {    title: 'ns',    dataIndex: 'namespaceIndex',    key: 'namespaceIndex',    align: 'center',  ellipsis: true,  show:false },
  {    title: 'identifier',    dataIndex: 'identifier',    key: 'identifier',    align: 'center',    show:true },
  {    title: '访问类型',    dataIndex: 'accessType',    key: 'accessType',    align: 'center',  show:false  },
  {    title: '数据类型',    dataIndex: 'dataType',    key: 'dataType',    align: 'center',  show:true  },
  {    title: '读写操作',    dataIndex: 'operate',    key: 'operate',    align: 'center',  show:true  },
  {    title: '备注',    dataIndex: 'remark',    key: 'remark',    align: 'center',  show:true  },
  {    title: '其他',    dataIndex: 'hardwareType',    key: 'hardwareType',    align: 'center',  show:false  },
])

const columnsCheck = (checked,data,index) => {
  data[index].show = checked
}


function handleResizeColumn(w, col) {
  col.width = w;
}
const rowSelection = {
  onChange: (selectedRowKeys, selectedRows) => {
    console.log(`selectedRowKeys: ${selectedRowKeys}`, 'selectedRows: ', selectedRows);
    selectRow.value = selectedRows
    console.log(selectRow.value.length)
  },
  getCheckboxProps: record => ({
    disabled: record.name === 'Disabled User',
    name: record.name,
  }),
};

const handlePageChange = (page, pageSize) => {
  search(page)
}

const handleMenuClick = e => {
  console.log('click', e);
};


const del = () =>{
  if (selectRow.value.length === 0){
    message.warn("请选中删除行")
    return ;
  }
  let arr = selectRow.value.map( item => item.id);
  DelOpcUa(arr).then(res => {
    if (res.code === 200){
      message.success("删除成功！")
      search(null)
    }
  })
}

const editModal = ref(false)
const showM = (name) => {
  let length = selectRow.value.length
  if (name === 'editModal' && length > 0){
    route.push( '/dataPoint/opcUa/Edit'+ selectRow.value[0].id)
  }

}

const edit = () =>{

}
const route = useRouter()
const add = () =>{
  route.push( '/dataPoint/opcUa/Edit'+ '0')
}
const read = () =>{

}
const write = () =>{

}
const importExcel = (e) =>{
  const formData = new FormData();
  formData.append('file', e.file);
  console.log(formData)
  OpcImportExcel(formData).then((res) => {
    if (res.code === 200) {
      message.success("保存成功")
      search(null)
    }
    else
      message.error(res.msg)
  })
}
const exportExcel = () =>{
  let query = {name:name.value, category: category.value, ip: ip.value, identifier: identifier.value,
    pageNum: paginationer.current, pageSize: paginationer.pageSize }
  OpcExportExcel(query).then(res =>{
    fileDownload(res, "opc数据点.xlsx");
  })

}

</script>

<template>
  <a-card>
    <a-flex :gap="50">
      <a-flex gap="middle">
        <label class="from-label">名称：</label>
        <a-input v-model:value="name" placeholder="请输入名称" />
      </a-flex>

      <a-flex gap="middle">
        <label class="from-label">分类：</label>
        <a-input v-model:value="category" placeholder="请输入类名" />
      </a-flex>
      <a-flex gap="middle">
        <label class="from-label">IP：</label>
        <a-input v-model:value="ip" placeholder="请输入ip" />
      </a-flex>

      <a-flex gap="middle">
        <label class="from-label">Identifier：</label>
        <a-input v-model:value="identifier" placeholder="请输入identifier" />
      </a-flex>

      <a-flex gap="large">
        <a-button type="primary" @click="search('')" block> <SearchOutlined />查询</a-button>
        <a-button block @click="reset"> <RedoOutlined />重置</a-button>
      </a-flex>

    </a-flex>


  </a-card>
  <a-card style="margin-top: 10px;">
    <a-popconfirm
        title="是否确定删除？"
        ok-text="Yes"
        cancel-text="No"
        @confirm="del"
    >
      <a-button type="primary" danger  style="margin: 10px;">
        <DeleteOutlined />
        删除
      </a-button>
    </a-popconfirm>

    <a-button type="primary"  @click="showM('editModal')"  style="margin: 10px;">
      编辑
    </a-button>
    <a-button type="primary"  @click="add"  style="margin: 10px;">
      新增
    </a-button>
    <!--    <a-button type="primary"  @click="read"  style="margin: 10px;">
          读取
        </a-button>
        <a-button type="primary"  @click="write"  style="margin: 10px;">
          写入
        </a-button>-->
    <a-upload
        :customRequest="importExcel"
        :showUploadList="false"
    >
      <a-button type="primary">
        <upload-outlined></upload-outlined>
        导入
      </a-button>
    </a-upload>
    <a-button type="primary"  @click="exportExcel"  style="margin: 10px;">
      导出
    </a-button>

    <a-dropdown>
      <template #overlay>
        <a-menu @click="handleMenuClick">
          <a-menu slot="overlay">
            <a-menu-item v-for="(item,index) in columns" :key="index"><a-checkbox :checked="item.show" @change="(e)=>{columnsCheck(e.target.checked,columns,index)}">{{item.title}}</a-checkbox></a-menu-item>
          </a-menu>
        </a-menu>
      </template>
      <a-button>
        筛选列
      </a-button>
    </a-dropdown>
    <a-table :data-source="dataSource" :columns="columns.filter((col,num)=>{if(col.show){return col}})" row-key="id"   size="middle" bordered
             :row-class-name="(_record, index) => (index % 2 === 1 ? 'table-striped' : null)"
             :row-selection="rowSelection"  @change="handlePageChange"
             :pagination="paginationer"  @resizeColumn="handleResizeColumn" >
      <template #bodyCell="{ column, text, record }">
        <template  v-if="column.dataIndex === 'operate'">
          <a-tag v-if="record.operate === 0" color="#87d068">Read</a-tag>
          <a-tag v-else color="#1677ff">Write</a-tag>
        </template>
        <template v-else-if="column.dataIndex === 'name'">
          <a-tooltip placement="topLeft" :title="record.name" >{{record.name}}</a-tooltip>
        </template>
        <template v-else-if="column.dataIndex === 'remark'">
          <a-tooltip placement="topLeft" :title="record.remark" >{{record.remark}}</a-tooltip>
        </template>

      </template>

    </a-table>

  </a-card>


</template>

<style scoped>

@media screen and (min-width:1800px) {
  .ant-input {
    width: 200px;
  }
  .from-label{
    font-weight: bold;
    line-height: 32px;
  }
}
@media screen and (min-width:1024px) and (max-width:1800px) {
  .from-label{
    font-weight: bold;
  }
  .ant-input {
    width: 100px;
    height: 32px;
  }
  .ant-picker{
    width: 200px;
  }
}

</style>